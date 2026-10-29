using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.DTOs;

namespace SalesInventory.Tests.Integration;

public class ProductsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_ValidCommand_CreatesAndReturnsProduct()
    {
        var command = new CreateProductCommand(
            $"Product {Guid.NewGuid()}",
            19.99m,
            50);

        var response = await _client.PostAsJsonAsync("/api/products", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be(command.Name);
        created.Price.Should().Be(19.99m);
        created.StockQuantity.Should().Be(50);
        created.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProducts_WithSearchAndPagination_ReturnsFilteredPagedResult()
    {
        var uniquePrefix = $"SearchTest_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products", new CreateProductCommand($"{uniquePrefix}_Alpha", 10m, 10));
        await _client.PostAsJsonAsync("/api/products", new CreateProductCommand($"{uniquePrefix}_Beta", 20m, 20));
        await _client.PostAsJsonAsync("/api/products", new CreateProductCommand($"{uniquePrefix}_Gamma", 30m, 30));

        var response = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?searchTerm={uniquePrefix}&page=1&pageSize=2");

        response.Should().NotBeNull();
        response!.TotalCount.Should().Be(3);
        response.Items.Should().HaveCount(2);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(2);
        response.TotalPages.Should().Be(2);

        var page2Response = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?searchTerm={uniquePrefix}&page=2&pageSize=2");

        page2Response.Should().NotBeNull();
        page2Response!.Items.Should().HaveCount(1);
        page2Response.Page.Should().Be(2);
    }

    [Fact]
    public async Task DeleteProduct_WithoutOrders_Succeeds()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Deletable Product {Guid.NewGuid()}",
            15.00m,
            10));

        var product = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync($"/api/products/{product!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_WithHistoricalOrders_ReturnsConflict()
    {
        var createProductResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(
            $"Referenced Product {Guid.NewGuid()}",
            35.00m,
            10));

        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product!.Id, Quantity = 2 }
            }));

        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var deleteResponse = await _client.DeleteAsync($"/api/products/{product.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
