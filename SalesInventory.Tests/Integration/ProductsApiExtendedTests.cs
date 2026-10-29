using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.DTOs;

namespace SalesInventory.Tests.Integration;

public class ProductsApiExtendedTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsApiExtendedTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

[Fact]
    public async Task GetProduct_ExistingId_Returns200AndProductDto()
    {
        var created = await CreateProductAsync($"GetById_Test_{Guid.NewGuid()}", 29.99m, 15);

        var response = await _client.GetAsync($"/api/products/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(created.Id);
        dto.Name.Should().Be(created.Name);
        dto.Price.Should().Be(29.99m);
        dto.StockQuantity.Should().Be(15);
        dto.CreatedAt.Should().NotBe(default);
        dto.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task GetProduct_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Not Found", because: "404 responses should surface the ProblemDetails title");
    }

[Fact]
    public async Task CreateProduct_EmptyName_Returns400WithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("", 10.00m, 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Name", because: "validation errors must identify the field");
    }

    [Fact]
    public async Task CreateProduct_WhitespaceName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("   ", 10.00m, 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_NegativePrice_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand($"NegPrice_{Guid.NewGuid()}", -0.01m, 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Price", because: "validation errors must identify the field");
    }

    [Fact]
    public async Task CreateProduct_ZeroPrice_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand($"FreeProduct_{Guid.NewGuid()}", 0.00m, 10));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_NegativeStock_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand($"NegStock_{Guid.NewGuid()}", 10.00m, -1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_ZeroStock_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand($"OutOfStock_{Guid.NewGuid()}", 5.00m, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_Returns201WithLocationHeader()
    {
        var name = $"LocationHeader_{Guid.NewGuid()}";
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand(name, 9.99m, 3));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        dto.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(dto!.Id.ToString(),
            because: "Location header must point to the created resource");
    }

    [Fact]
    public async Task CreateProduct_DecimalPrice_PersistedWithPrecision()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand($"Decimal_{Guid.NewGuid()}", 12.34m, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        dto!.Price.Should().Be(12.34m);
    }

    [Fact]
    public async Task CreateProduct_NameExceeding200Chars_Returns400()
    {
        var longName = new string('A', 201);
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand(longName, 10.00m, 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

[Fact]
    public async Task UpdateProduct_ValidCommand_Returns200WithUpdatedDto()
    {
        var created = await CreateProductAsync($"Update_Before_{Guid.NewGuid()}", 10.00m, 5);

        var updateCmd = new UpdateProductCommand(created.Id, $"Update_After_{Guid.NewGuid()}", 99.99m, 20);
        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}", updateCmd);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be(updateCmd.Name);
        updated.Price.Should().Be(99.99m);
        updated.StockQuantity.Should().Be(20);
    }

    [Fact]
    public async Task UpdateProduct_NonExistentId_Returns404()
    {
        var missingId = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync($"/api/products/{missingId}",
            new UpdateProductCommand(missingId, "X", 1.00m, 1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_EmptyName_Returns400()
    {
        var created = await CreateProductAsync($"UpdateBadName_{Guid.NewGuid()}", 10.00m, 5);

        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}",
            new UpdateProductCommand(created.Id, "", 10.00m, 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var fetched = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{created.Id}");
        fetched!.StockQuantity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateProduct_NegativePrice_Returns400AndLeavesProductUnchanged()
    {
        var created = await CreateProductAsync($"UpdateNegPrice_{Guid.NewGuid()}", 10.00m, 5);

        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}",
            new UpdateProductCommand(created.Id, created.Name, -1.00m, 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var fetched = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{created.Id}");
        fetched!.Price.Should().Be(10.00m);
    }

[Fact]
    public async Task DeleteProduct_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

[Fact]
    public async Task GetProducts_InvalidPage_Returns400()
    {
        var response = await _client.GetAsync("/api/products?page=0&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProducts_InvalidPageSize_Returns400()
    {
        var response = await _client.GetAsync("/api/products?page=1&pageSize=0");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProducts_PageSizeExceeds100_Returns400()
    {
        var response = await _client.GetAsync("/api/products?page=1&pageSize=101");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProducts_SearchWithNoResults_ReturnsEmptyPagedResult()
    {
        var searchTerm = $"ZZZNO_MATCH_{Guid.NewGuid():N}";
        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?searchTerm={searchTerm}");

        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetProducts_PageBeyondEnd_ReturnsEmptyItemsWithCorrectMetadata()
    {
        var prefix = $"PageBeyond_{Guid.NewGuid():N}";
        await CreateProductAsync($"{prefix}_Only", 5.00m, 1);

        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?searchTerm={prefix}&page=99&pageSize=10");

        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(99);
    }

    [Fact]
    public async Task GetProducts_SearchIsCaseInsensitivePerSqlServerCollation()
    {
        var prefix = $"CaseTest_{Guid.NewGuid():N}";
        await CreateProductAsync($"{prefix}_Widget", 10.00m, 5);

        var upper = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?searchTerm={prefix.ToUpperInvariant()}_WIDGET");

        upper.Should().NotBeNull();
        upper!.TotalCount.Should().Be(1,
            because: "SQL Server default collation is case-insensitive");
    }

    [Fact]
    public async Task GetProducts_SearchByProductGuid_ReturnsMatchingProduct()
    {
        var product = await CreateProductAsync($"SearchById_{Guid.NewGuid():N}", 12.50m, 7);

        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?searchTerm={product.Id}");

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(p => p.Id == product.Id);
    }

    [Fact]
    public async Task GetProducts_MetadataHasNextAndPreviousPageFlags()
    {
        var prefix = $"PagingFlags_{Guid.NewGuid():N}";
        await CreateProductAsync($"{prefix}_A", 1.00m, 1);
        await CreateProductAsync($"{prefix}_B", 2.00m, 1);
        await CreateProductAsync($"{prefix}_C", 3.00m, 1);

        var page1 = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?searchTerm={prefix}&page=1&pageSize=2");
        page1!.HasNextPage.Should().BeTrue();
        page1.HasPreviousPage.Should().BeFalse();

        var page2 = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            $"/api/products?searchTerm={prefix}&page=2&pageSize=2");
        page2!.HasNextPage.Should().BeFalse();
        page2.HasPreviousPage.Should().BeTrue();
    }

[Fact]
    public async Task CreateProduct_ValidationError_ResponseIsProblemJson()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("", -1.00m, -1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("title", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetProduct_NotFound_ResponseIsProblemJson()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("title", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("detail", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_DuplicateName_Returns409Conflict()
    {
        var name = $"Unique_Name_{Guid.NewGuid():N}";
        var first = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(name, 10.00m, 5));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(name, 20.00m, 10));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateProduct_DuplicateNameWithDifferentCasing_Returns409Conflict()
    {
        var baseName = $"Case_Test_{Guid.NewGuid():N}";
        var first = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(baseName.ToLower(), 10.00m, 5));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(baseName.ToUpper(), 20.00m, 10));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateProduct_DuplicateNameWithSurroundingWhitespace_Returns409Conflict()
    {
        var baseName = $"Whitespace_Test_{Guid.NewGuid():N}";
        var first = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand(baseName, 10.00m, 5));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand($"   {baseName}   ", 20.00m, 10));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProduct_SameName_Succeeds()
    {
        var name = $"KeepName_{Guid.NewGuid():N}";
        var product = await CreateProductAsync(name, 10.00m, 5);

        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{product.Id}",
            new UpdateProductCommand(product.Id, name, 25.00m, 15));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDto>();
        updated!.Price.Should().Be(25.00m);
        updated.StockQuantity.Should().Be(15);
        updated.Name.Should().Be(name);
    }

    [Fact]
    public async Task UpdateProduct_NameToExistingAnotherProductName_Returns409Conflict()
    {
        var name1 = $"Prod1_{Guid.NewGuid():N}";
        var name2 = $"Prod2_{Guid.NewGuid():N}";
        var p1 = await CreateProductAsync(name1, 10.00m, 5);
        var p2 = await CreateProductAsync(name2, 20.00m, 10);

        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{p2.Id}",
            new UpdateProductCommand(p2.Id, name1, 30.00m, 10));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProduct_NameToExistingAnotherProductName_DifferentCasing_Returns409Conflict()
    {
        var name1 = $"ProdCasing_{Guid.NewGuid():N}";
        var name2 = $"ProdOther_{Guid.NewGuid():N}";
        var p1 = await CreateProductAsync(name1, 10.00m, 5);
        var p2 = await CreateProductAsync(name2, 20.00m, 10);

        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{p2.Id}",
            new UpdateProductCommand(p2.Id, name1.ToUpper(), 30.00m, 10));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<ProductDto> CreateProductAsync(string name, decimal price, int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand(name, price, stock));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }
}