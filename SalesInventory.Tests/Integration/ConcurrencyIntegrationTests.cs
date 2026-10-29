using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Infrastructure.Persistence;

namespace SalesInventory.Tests.Integration;

public class ConcurrencyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConcurrencyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConcurrentOrders_CannotOversellInventory()
    {
        var setupClient = _factory.CreateClient();
        var createProductResponse = await setupClient.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Concurrency Product {Guid.NewGuid()}",
            10.00m,
            10));

        createProductResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        var productId = product!.Id;

        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            var client = _factory.CreateClient();
            return await client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
                new List<CreateOrderItemRequest>
                {
                    new() { ProductId = productId, Quantity = 4 }
                }));
        });

        var responses = await Task.WhenAll(tasks);

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        successCount.Should().BeInRange(1, 2);
        conflictCount.Should().Be(5 - successCount);

        var updatedProduct = await setupClient.GetFromJsonAsync<ProductDto>($"/api/products/{productId}");
        updatedProduct.Should().NotBeNull();
        updatedProduct!.StockQuantity.Should().Be(10 - (successCount * 4));
    }

    [Fact]
    public async Task ConcurrentCancellations_CannotRestoreStockTwice()
    {
        var setupClient = _factory.CreateClient();
        var createProductResponse = await setupClient.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Cancel Concurrency Product {Guid.NewGuid()}",
            15.00m,
            10));

        createProductResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();

        var createOrderResponse = await setupClient.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product!.Id, Quantity = 5 }
            }));

        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await createOrderResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();

        var productAfterOrder = await setupClient.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        productAfterOrder!.StockQuantity.Should().Be(5);

        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            var client = _factory.CreateClient();
            return await client.PostAsync($"/api/orders/{order!.Id}/cancel", null);
        });

        var responses = await Task.WhenAll(tasks);

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.NoContent);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        successCount.Should().Be(1);
        conflictCount.Should().Be(4);

        var productAfterCancel = await setupClient.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        productAfterCancel.Should().NotBeNull();
        productAfterCancel!.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task MultiProductOrder_ConcurrencyConflict_RollsBackAllStockDeductions()
    {
        var setupClient = _factory.CreateClient();
        var product1Response = await setupClient.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Multi Conflict Product 1 {Guid.NewGuid()}",
            20.00m,
            10));
        var product2Response = await setupClient.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Multi Conflict Product 2 {Guid.NewGuid()}",
            30.00m,
            10));

        var product1 = await product1Response.Content.ReadFromJsonAsync<ProductDto>();
        var product2 = await product2Response.Content.ReadFromJsonAsync<ProductDto>();

        var task1 = Task.Run(async () =>
        {
            var client = _factory.CreateClient();
            return await client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
                new List<CreateOrderItemRequest>
                {
                    new() { ProductId = product1!.Id, Quantity = 3 },
                    new() { ProductId = product2!.Id, Quantity = 3 }
                }));
        });

        var task2 = Task.Run(async () =>
        {
            var client = _factory.CreateClient();
            return await client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
                new List<CreateOrderItemRequest>
                {
                    new() { ProductId = product2!.Id, Quantity = 4 }
                }));
        });

        var responses = await Task.WhenAll(task1, task2);

        var p1After = await setupClient.GetFromJsonAsync<ProductDto>($"/api/products/{product1!.Id}");
        var p2After = await setupClient.GetFromJsonAsync<ProductDto>($"/api/products/{product2!.Id}");

        var order1Success = responses[0].StatusCode == HttpStatusCode.Created;
        var order2Success = responses[1].StatusCode == HttpStatusCode.Created;

        if (order1Success && !order2Success)
        {
            p1After!.StockQuantity.Should().Be(7);
            p2After!.StockQuantity.Should().Be(7);
        }
        else if (!order1Success && order2Success)
        {
            p1After!.StockQuantity.Should().Be(10);
            p2After!.StockQuantity.Should().Be(6);
        }
        else if (order1Success && order2Success)
        {
            p1After!.StockQuantity.Should().Be(7);
            p2After!.StockQuantity.Should().Be(3);
        }
    }

    [Fact]
    public async Task FailedCancellation_LeavesOrderAndInventoryUnchanged()
    {
        var setupClient = _factory.CreateClient();
        var createProductResponse = await setupClient.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Failed Cancel Product {Guid.NewGuid()}",
            25.00m,
            10));

        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductDto>();

        var createOrderResponse = await setupClient.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product!.Id, Quantity = 4 }
            }));

        var order = await createOrderResponse.Content.ReadFromJsonAsync<OrderDto>();

        var cancelResponse1 = await setupClient.PostAsync($"/api/orders/{order!.Id}/cancel", null);
        cancelResponse1.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cancelResponse2 = await setupClient.PostAsync($"/api/orders/{order.Id}/cancel", null);
        cancelResponse2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var productAfterFailedCancel = await setupClient.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        productAfterFailedCancel!.StockQuantity.Should().Be(10);

        var fetchedOrder = await setupClient.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        fetchedOrder!.Status.Should().Be(Domain.Enums.OrderStatus.Cancelled);
    }

    [Fact]
    public async Task ConcurrentProductCreation_SameName_ExactlyOneSucceedsAndOthersGetConflict()
    {
        var uniqueName = $"Concurrent_Unique_Prod_{Guid.NewGuid():N}";
        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            var client = _factory.CreateClient();
            return await client.PostAsJsonAsync("/api/products", new CreateProductCommand(uniqueName, 15.00m, 10));
        });

        var responses = await Task.WhenAll(tasks);

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        successCount.Should().Be(1, because: "only one product creation can succeed with the unique name");
        conflictCount.Should().Be(4, because: "the other 4 requests must fail with 409 Conflict due to unique constraint");
    }
}
