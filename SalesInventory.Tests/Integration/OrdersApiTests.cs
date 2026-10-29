using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Domain.Enums;

namespace SalesInventory.Tests.Integration;

public class OrdersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteOrderWorkflow_SucceedsWithStockManagementAndCancellation()
    {
        var createProductResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Integration Test Product {Guid.NewGuid()}",
            25.50m,
            10));

        createProductResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        var productId = product!.Id;

        var insufficientOrderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 15 }
            }));

        insufficientOrderResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var productAfterFailedOrder = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{productId}");
        productAfterFailedOrder.Should().NotBeNull();
        productAfterFailedOrder!.StockQuantity.Should().Be(10);

        var validOrderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 4 }
            }));

        validOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await validOrderResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.TotalAmount.Should().Be(102.00m);
        order.Status.Should().Be(OrderStatus.Completed);

        var productAfterValidOrder = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{productId}");
        productAfterValidOrder.Should().NotBeNull();
        productAfterValidOrder!.StockQuantity.Should().Be(6);

        var cancelResponse = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var productAfterCancel = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{productId}");
        productAfterCancel.Should().NotBeNull();
        productAfterCancel!.StockQuantity.Should().Be(10);

        var doubleCancelResponse = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        doubleCancelResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var productAfterDoubleCancel = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{productId}");
        productAfterDoubleCancel.Should().NotBeNull();
        productAfterDoubleCancel!.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task HistoricalUnitPrice_RemainsUnchanged_WhenProductPriceChanges()
    {
        var createProductResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Price Snapshot Product {Guid.NewGuid()}",
            50.00m,
            20));

        createProductResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product!.Id, Quantity = 2 }
            }));

        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.TotalAmount.Should().Be(100.00m);
        order.Items.First().UnitPrice.Should().Be(50.00m);

        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{product.Id}", new UpdateProductCommand(
            product.Id,
            product.Name,
            150.00m,
            product.StockQuantity));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedOrder = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        fetchedOrder.Should().NotBeNull();
        fetchedOrder!.TotalAmount.Should().Be(100.00m);
        fetchedOrder.Items.First().UnitPrice.Should().Be(50.00m);
    }

    [Fact]
    public async Task CreateOrder_WithDuplicateProductIds_AggregatesAndPersistsCorrectly()
    {
        var createProductResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Duplicate Test Product {Guid.NewGuid()}",
            10.00m,
            20));

        createProductResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product!.Id, Quantity = 2 },
                new() { ProductId = product.Id, Quantity = 3 }
            }));

        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.TotalAmount.Should().Be(50.00m);
        order.Items.Should().HaveCount(1);
        order.Items.First().Quantity.Should().Be(5);

        var updatedProduct = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        updatedProduct.Should().NotBeNull();
        updatedProduct!.StockQuantity.Should().Be(15);
    }

    [Fact]
    public async Task CreateOrder_MultiProductFailure_RollsBackAllStockDeductions()
    {
        var product1Response = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Multi Rollback Product 1 {Guid.NewGuid()}",
            20.00m,
            10));
        var product2Response = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Multi Rollback Product 2 {Guid.NewGuid()}",
            30.00m,
            2));

        var product1 = await product1Response.Content.ReadFromJsonAsync<ProductDto>();
        var product2 = await product2Response.Content.ReadFromJsonAsync<ProductDto>();

        var failedOrderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product1!.Id, Quantity = 5 },
                new() { ProductId = product2!.Id, Quantity = 10 }
            }));

        failedOrderResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var product1After = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product1!.Id}");
        var product2After = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product2!.Id}");

        product1After.Should().NotBeNull();
        product2After.Should().NotBeNull();
        product1After!.StockQuantity.Should().Be(10);
        product2After!.StockQuantity.Should().Be(2);
    }

    [Fact]
    public async Task CreateOrder_InvalidQuantity_ReturnsBadRequest()
    {
        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 0 }
            }));

        orderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_MissingProduct_ReturnsNotFound()
    {
        var nonExistentProductId = Guid.NewGuid();
        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = nonExistentProductId, Quantity = 1 }
            }));

        orderResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelOrder_NonExistentOrder_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PostAsync($"/api/orders/{nonExistentId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
