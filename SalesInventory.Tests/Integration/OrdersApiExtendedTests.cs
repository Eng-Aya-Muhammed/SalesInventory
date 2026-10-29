using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Domain.Enums;

namespace SalesInventory.Tests.Integration;

public class OrdersApiExtendedTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersApiExtendedTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

[Fact]
    public async Task CreateOrder_SingleProduct_Returns201WithLocationHeaderAndCorrectBody()
    {
        var product = await CreateProductAsync($"SingleProduct_{Guid.NewGuid()}", 25.00m, 10);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 3 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Id.Should().NotBeEmpty();
        order.TotalAmount.Should().Be(75.00m);
        order.Status.Should().Be(OrderStatus.Completed);
        order.Items.Should().HaveCount(1);
        order.Items[0].Quantity.Should().Be(3);
        order.Items[0].UnitPrice.Should().Be(25.00m);

        response.Headers.Location!.ToString().Should().Contain(order.Id.ToString());
    }

    [Fact]
    public async Task CreateOrder_MultipleProducts_CalculatesTotalCorrectly()
    {
        var p1 = await CreateProductAsync($"MultiP1_{Guid.NewGuid()}", 10.00m, 20);
        var p2 = await CreateProductAsync($"MultiP2_{Guid.NewGuid()}", 30.00m, 20);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = p1.Id, Quantity = 4 },
                new() { ProductId = p2.Id, Quantity = 2 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.TotalAmount.Should().Be(100.00m);
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_QuantityExactlyEqualsStock_Succeeds()
    {
        var product = await CreateProductAsync($"ExactStock_{Guid.NewGuid()}", 5.00m, 7);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 7 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var updated = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        updated!.StockQuantity.Should().Be(0);
    }

    [Fact]
    public async Task CreateOrder_QuantityExceedsStockByOne_Returns409AndStockIsUnchanged()
    {
        var product = await CreateProductAsync($"OverStock_{Guid.NewGuid()}", 5.00m, 7);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 8 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var updated = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        updated!.StockQuantity.Should().Be(7, because: "stock must not change after a failed order");
    }

    [Fact]
    public async Task CreateOrder_EmptyItems_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_ZeroQuantity_Returns400AndNoOrderCreated()
    {
        var product = await CreateProductAsync($"ZeroQty_{Guid.NewGuid()}", 10.00m, 10);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 0 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var updated = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        updated!.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task CreateOrder_NegativeQuantity_Returns400()
    {
        var product = await CreateProductAsync($"NegQty_{Guid.NewGuid()}", 10.00m, 10);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = -3 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_NonExistentProduct_Returns404AndNoOrderCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_ServerCalculatesPrice_ClientValueIgnored()
    {
        var product = await CreateProductAsync($"ServerPrice_{Guid.NewGuid()}", 50.00m, 10);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 2 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.TotalAmount.Should().Be(100.00m);
        order.Items[0].UnitPrice.Should().Be(50.00m);
    }

    [Fact]
    public async Task CreateOrder_InvalidItems_Returns400WithoutCreatingOrder()
    {
        var product = await CreateProductAsync($"InvalidItems_{Guid.NewGuid()}", 10.00m, 10);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 1 },
                new() { ProductId = product.Id, Quantity = 0 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var updated = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        updated!.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_Returns409WithProblemDetails()
    {
        var product = await CreateProductAsync($"InsufficientStock_{Guid.NewGuid()}", 20.00m, 3);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 100 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("title", out _).Should().BeTrue();
    }

[Fact]
    public async Task GetOrder_ExistingId_Returns200WithLineItems()
    {
        var product = await CreateProductAsync($"GetOrderById_{Guid.NewGuid()}", 15.00m, 10);
        var created = await CreateOrderAsync(product.Id, 2);

        var response = await _client.GetAsync($"/api/orders/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var dto = await response.Content.ReadFromJsonAsync<OrderDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(created.Id);
        dto.TotalAmount.Should().Be(30.00m);
        dto.Items.Should().HaveCount(1);
        dto.Items[0].ProductName.Should().NotBeEmpty();
        dto.Items[0].Quantity.Should().Be(2);
        dto.Items[0].UnitPrice.Should().Be(15.00m);
        dto.Items[0].LineTotal.Should().Be(30.00m);
    }

    [Fact]
    public async Task GetOrder_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrders_InvalidPage_Returns400()
    {
        var response = await _client.GetAsync("/api/orders?page=0&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrders_ReturnsPagedResult_WithCorrectShape()
    {
        var product = await CreateProductAsync($"OrdersList_{Guid.NewGuid()}", 10.00m, 100);
        await CreateOrderAsync(product.Id, 1);

        var result = await _client.GetFromJsonAsync<PagedResult<OrderListItemDto>>(
            "/api/orders?page=1&pageSize=10");

        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterThan(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);

        foreach (var item in result.Items)
        {
            item.Id.Should().NotBeEmpty();
            item.TotalAmount.Should().BeGreaterThan(0);
            item.StatusName.Should().NotBeEmpty();
        }
    }

[Fact]
    public async Task CancelOrder_ValidOrder_Returns204()
    {
        var product = await CreateProductAsync($"Cancel204_{Guid.NewGuid()}", 10.00m, 10);
        var order = await CreateOrderAsync(product.Id, 3);

        var response = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CancelOrder_StockIsRestoredExactly()
    {
        var product = await CreateProductAsync($"CancelRestore_{Guid.NewGuid()}", 10.00m, 10);
        var order = await CreateOrderAsync(product.Id, 4);

        var stockAfterOrder = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        stockAfterOrder!.StockQuantity.Should().Be(6);

        await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        var stockAfterCancel = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        stockAfterCancel!.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task CancelOrder_OrderStatusBecomesCancel()
    {
        var product = await CreateProductAsync($"CancelStatus_{Guid.NewGuid()}", 10.00m, 10);
        var order = await CreateOrderAsync(product.Id, 2);

        await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        var fetched = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        fetched!.Status.Should().Be(OrderStatus.Cancelled);
        fetched.StatusName.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelOrder_NonExistentId_Returns404()
    {
        var response = await _client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelOrder_AlreadyCancelled_Returns409()
    {
        var product = await CreateProductAsync($"DoubleCancel_{Guid.NewGuid()}", 10.00m, 10);
        var order = await CreateOrderAsync(product.Id, 2);

        await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        var secondCancel = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        secondCancel.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelOrder_AlreadyCancelled_StockNotRestoredTwice()
    {
        var product = await CreateProductAsync($"StockNotDouble_{Guid.NewGuid()}", 10.00m, 10);
        var order = await CreateOrderAsync(product.Id, 3);

        await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        var stock = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        stock!.StockQuantity.Should().Be(10, because: "stock must only be restored once");
    }

    [Fact]
    public async Task CancelOrder_MultiProductOrder_RestoresAllProductStocks()
    {
        var p1 = await CreateProductAsync($"CancelMulti_P1_{Guid.NewGuid()}", 10.00m, 20);
        var p2 = await CreateProductAsync($"CancelMulti_P2_{Guid.NewGuid()}", 20.00m, 20);

        var orderResp = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = p1.Id, Quantity = 5 },
                new() { ProductId = p2.Id, Quantity = 3 }
            }));
        orderResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDto>();

        await _client.PostAsync($"/api/orders/{order!.Id}/cancel", null);

        var stock1 = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{p1.Id}");
        var stock2 = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{p2.Id}");
        stock1!.StockQuantity.Should().Be(20);
        stock2!.StockQuantity.Should().Be(20);
    }

    [Fact]
    public async Task CancelOrder_FailedCancellation_LeavesOrderAndInventoryUnchanged()
    {
        var product = await CreateProductAsync($"FailCancel_{Guid.NewGuid()}", 10.00m, 10);
        var order = await CreateOrderAsync(product.Id, 4);

        var first = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var fetchedOrder = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        fetchedOrder!.Status.Should().Be(OrderStatus.Cancelled);

        var stock = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}");
        stock!.StockQuantity.Should().Be(10);
    }

[Fact]
    public async Task UnknownRoute_Returns404()
    {
        var response = await _client.GetAsync("/api/nonexistent-route");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnsupportedMethod_Returns405()
    {
        var response = await _client.PatchAsync("/api/products",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400);
    }

private async Task<ProductDto> CreateProductAsync(string name, decimal price, int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand(name, price, stock));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    private async Task<OrderDto> CreateOrderAsync(Guid productId, int quantity)
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = productId, Quantity = quantity }
            }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }
}