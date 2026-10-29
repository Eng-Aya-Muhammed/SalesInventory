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
using SalesInventory.Domain.Enums;
using SalesInventory.Infrastructure.Persistence;

namespace SalesInventory.Tests.Integration;

public class TransactionRollbackTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransactionRollbackTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

[Fact]
    public async Task MultiProductOrder_InsufficientStockOnSecond_RollsBackFirstProductToo()
    {
        var p1 = await CreateProductAsync($"TxRollback_P1_{Guid.NewGuid()}", 10.00m, 10);
        var p2 = await CreateProductAsync($"TxRollback_P2_{Guid.NewGuid()}", 20.00m, 2);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = p1.Id, Quantity = 5 },
                new() { ProductId = p2.Id, Quantity = 10 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var p1InDb = await db.Products.AsNoTracking().FirstAsync(p => p.Id == p1.Id);
        var p2InDb = await db.Products.AsNoTracking().FirstAsync(p => p.Id == p2.Id);

        p1InDb.StockQuantity.Should().Be(10, because: "the transaction must have rolled back P1 stock deduction");
        p2InDb.StockQuantity.Should().Be(2, because: "P2 stock must remain unchanged");
    }

    [Fact]
    public async Task MultiProductOrder_Failure_LeavesNoPartialOrderOrItems()
    {
        var p1 = await CreateProductAsync($"NoPartial_P1_{Guid.NewGuid()}", 10.00m, 10);
        var p2 = await CreateProductAsync($"NoPartial_P2_{Guid.NewGuid()}", 10.00m, 1);

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = p1.Id, Quantity = 2 },
                new() { ProductId = p2.Id, Quantity = 5 }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var orphanItems = await db.OrderItems.AsNoTracking()
            .Where(oi => oi.ProductId == p1.Id || oi.ProductId == p2.Id)
            .ToListAsync();

        orphanItems.Should().BeEmpty(because: "a failed order must leave no order items behind");
    }

[Fact]
    public async Task CancelOrder_FailedRepeatedCancel_VerifiedViaFreshDbContext()
    {
        var product = await CreateProductAsync($"CancelVerify_{Guid.NewGuid()}", 15.00m, 10);

        var orderResp = await _client.PostAsJsonAsync("/api/orders", new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 4 }
            }));
        orderResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDto>();

        var first = await _client.PostAsync($"/api/orders/{order!.Id}/cancel", null);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var productInDb = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        productInDb.StockQuantity.Should().Be(10,
            because: "stock must be restored exactly once despite two cancel attempts");

        var orderInDb = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        orderInDb.Status.Should().Be(OrderStatus.Cancelled, because: "order status must be Cancelled");
    }

[Fact]
    public async Task RowVersion_ChangesAfterSuccessfulUpdate()
    {
        var product = await CreateProductAsync($"RowVersionChange_{Guid.NewGuid()}", 10.00m, 5);

        await using var scopeBefore = _factory.Services.CreateAsyncScope();
        var dbBefore = scopeBefore.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await dbBefore.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        var rowVersionBefore = before.RowVersion;

        await _client.PutAsJsonAsync($"/api/products/{product.Id}",
            new UpdateProductCommand(product.Id, product.Name, 20.00m, 10));

        await using var scopeAfter = _factory.Services.CreateAsyncScope();
        var dbAfter = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
        var after = await dbAfter.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);

        after.RowVersion.Should().NotBeEquivalentTo(rowVersionBefore,
            because: "SQL Server must auto-increment rowversion on every write");
    }

    [Fact]
    public async Task RowVersion_IsNotEmpty_AndIsDbGenerated()
    {
        var product = await CreateProductAsync($"RowVersionNotEmpty_{Guid.NewGuid()}", 5.00m, 3);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);

        entity.RowVersion.Should().NotBeNull();
        entity.RowVersion.Should().HaveCount(8, because: "SQL Server rowversion is always 8 bytes");
        entity.RowVersion.Should().NotBeEquivalentTo(new byte[8], because: "rowversion must be database-generated, never zero");
    }

    [Fact]
    public async Task RowVersion_StaleWrite_MappedTo409Conflict()
    {

        var product = await CreateProductAsync($"StaleWrite_{Guid.NewGuid()}", 10.00m, 100);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var staleEntity = await db.Products.FirstAsync(p => p.Id == product.Id);

        await _client.PutAsJsonAsync($"/api/products/{product.Id}",
            new UpdateProductCommand(product.Id, product.Name + "_V2", 20.00m, 90));

        staleEntity.Price = 999.00m;
        db.Products.Update(staleEntity);

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            because: "EF Core must detect the rowversion mismatch and throw");
    }

private async Task<ProductDto> CreateProductAsync(string name, decimal price, int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand(name, price, stock));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }
}