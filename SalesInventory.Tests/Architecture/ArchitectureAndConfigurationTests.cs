using AutoMapper;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesInventory.Application;
using SalesInventory.Application.Behaviors;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.Queries;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.Queries;
using SalesInventory.Application.Interfaces;
using SalesInventory.Application.Mapping;
using SalesInventory.Infrastructure.Persistence;

namespace SalesInventory.Tests.Architecture;

public class ArchitectureAndConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IServiceProvider _services;

    public ArchitectureAndConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _services = factory.Services;
    }

[Fact]
    public void ServiceProvider_CanBeConstructed_WithoutErrors()
    {
        _services.Should().NotBeNull();
    }

    [Fact]
    public void IProductRepository_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetService<IProductRepository>();
        repo.Should().NotBeNull();
    }

    [Fact]
    public void IOrderRepository_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetService<IOrderRepository>();
        repo.Should().NotBeNull();
    }

    [Fact]
    public void IUnitOfWork_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
        uow.Should().NotBeNull();
    }

    [Fact]
    public void IMediator_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

[Fact]
    public void CreateProductCommandHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<CreateProductCommand, Application.Features.Products.DTOs.ProductDto>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProductCommandHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<UpdateProductCommand, Application.Features.Products.DTOs.ProductDto>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void DeleteProductCommandHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<DeleteProductCommand>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetProductsQueryHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<GetProductsQuery, Application.Features.Products.DTOs.PagedResult<Application.Features.Products.DTOs.ProductListItemDto>>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetProductByIdQueryHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<GetProductByIdQuery, Application.Features.Products.DTOs.ProductDto>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void CreateOrderCommandHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<CreateOrderCommand, Application.Features.Orders.DTOs.OrderDto>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void CancelOrderCommandHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<CancelOrderCommand>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetOrdersQueryHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<GetOrdersQuery, Application.Features.Products.DTOs.PagedResult<Application.Features.Orders.DTOs.OrderListItemDto>>>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetOrderByIdQueryHandler_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetService<IRequestHandler<GetOrderByIdQuery, Application.Features.Orders.DTOs.OrderDto>>();
        handler.Should().NotBeNull();
    }

[Fact]
    public void CreateProductCommandValidator_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<CreateProductCommand>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void UpdateProductCommandValidator_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<UpdateProductCommand>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void DeleteProductCommandValidator_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<DeleteProductCommand>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateOrderCommandValidator_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<CreateOrderCommand>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void CancelOrderCommandValidator_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<CancelOrderCommand>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void GetProductsQueryValidator_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<GetProductsQuery>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void GetOrdersQueryValidator_IsRegistered()
    {
        using var scope = _services.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<GetOrdersQuery>>();
        validators.Should().NotBeEmpty();
    }

[Fact]
    public void LoggingBehavior_IsRegisteredInPipeline()
    {
        using var scope = _services.CreateScope();
        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<CreateProductCommand, Application.Features.Products.DTOs.ProductDto>>();
        behaviors.Should().Contain(b => b is LoggingBehavior<CreateProductCommand, Application.Features.Products.DTOs.ProductDto>);
    }

    [Fact]
    public void ValidationBehavior_IsRegisteredInPipeline()
    {
        using var scope = _services.CreateScope();
        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<CreateProductCommand, Application.Features.Products.DTOs.ProductDto>>();
        behaviors.Should().Contain(b => b is ValidationBehavior<CreateProductCommand, Application.Features.Products.DTOs.ProductDto>);
    }

[Fact]
    public void AutoMapper_ConfigurationIsValid()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<OrderProfile>();
        });

        var act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow(because: "AutoMapper configuration must have no unresolved members");
    }

    [Fact]
    public void AutoMapper_IsRegisteredAndCanMap()
    {
        using var scope = _services.CreateScope();
        var mapper = scope.ServiceProvider.GetService<IMapper>();
        mapper.Should().NotBeNull();

        var product = new SalesInventory.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Price = 10.00m,
            StockQuantity = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var act = () => mapper!.Map<Application.Features.Products.DTOs.ProductDto>(product);
        act.Should().NotThrow();
    }

[Fact]
    public void AppDbContext_CanBeResolved()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetService<AppDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public async Task AppDbContext_CanConnectToDatabase()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var canConnect = await db.Database.CanConnectAsync();
        canConnect.Should().BeTrue(because: "the test database must be reachable");
    }

    [Fact]
    public async Task Migrations_AreApplied_NoPendingMigrations()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        pending.Should().BeEmpty(because: "all migrations must be applied before tests run");
    }

    [Fact]
    public void ProductConfiguration_HasRowVersionConfigured()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = db.Model.FindEntityType(typeof(SalesInventory.Domain.Entities.Product));
        entityType.Should().NotBeNull();

        var rowVersionProp = entityType!.FindProperty(nameof(SalesInventory.Domain.Entities.Product.RowVersion));
        rowVersionProp.Should().NotBeNull();
        rowVersionProp!.IsConcurrencyToken.Should().BeTrue(because: "RowVersion must be configured as concurrency token");
    }

    [Fact]
    public void ProductConfiguration_HasUniqueNameIndexConfigured()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = db.Model.FindEntityType(typeof(SalesInventory.Domain.Entities.Product));
        entityType.Should().NotBeNull();

        var index = entityType!.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(SalesInventory.Domain.Entities.Product.Name)));
        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue(because: "Product name must have a unique index");
    }

    [Fact]
    public void SalesOrderConfiguration_HasRowVersionConfigured()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = db.Model.FindEntityType(typeof(SalesInventory.Domain.Entities.SalesOrder));
        entityType.Should().NotBeNull();

        var rowVersionProp = entityType!.FindProperty(nameof(SalesInventory.Domain.Entities.SalesOrder.RowVersion));
        rowVersionProp.Should().NotBeNull();
        rowVersionProp!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void OrderItemConfiguration_HasProductForeignKey()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = db.Model.FindEntityType(typeof(SalesInventory.Domain.Entities.OrderItem));
        entityType.Should().NotBeNull();

        var fk = entityType!.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(SalesInventory.Domain.Entities.Product));
        fk.Should().NotBeNull(because: "OrderItem must have a FK to Product");
    }

[Fact]
    public void DomainAssembly_DoesNotReference_InfrastructureAssembly()
    {
        var domainAssembly = typeof(SalesInventory.Domain.Entities.Product).Assembly;
        var infraName = "SalesInventory.Infrastructure";

        var referencesInfra = domainAssembly.GetReferencedAssemblies()
            .Any(a => a.Name?.Contains(infraName, StringComparison.OrdinalIgnoreCase) == true);

        referencesInfra.Should().BeFalse(
            because: "the Domain layer must be framework-agnostic and must not reference Infrastructure");
    }

    [Fact]
    public void ApplicationAssembly_DoesNotReference_InfrastructureAssembly()
    {
        var appAssembly = typeof(DependencyInjection).Assembly;
        var infraName = "SalesInventory.Infrastructure";

        var referencesInfra = appAssembly.GetReferencedAssemblies()
            .Any(a => a.Name?.Contains(infraName, StringComparison.OrdinalIgnoreCase) == true);

        referencesInfra.Should().BeFalse(
            because: "the Application layer must not reference Infrastructure; use interfaces instead");
    }

    [Fact]
    public void ApplicationAssembly_DoesNotReference_ApiAssembly()
    {
        var appAssembly = typeof(DependencyInjection).Assembly;
        var apiName = "SalesInventory.Api";

        var referencesApi = appAssembly.GetReferencedAssemblies()
            .Any(a => a.Name?.Contains(apiName, StringComparison.OrdinalIgnoreCase) == true);

        referencesApi.Should().BeFalse(
            because: "the Application layer must not reference the API presentation layer");
    }
}