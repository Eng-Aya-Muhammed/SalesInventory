using FluentAssertions;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Orders.Queries;
using SalesInventory.Application.Features.Orders.Validators;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.Queries;
using SalesInventory.Application.Features.Products.Validators;

namespace SalesInventory.Tests.Unit.Validators;

public class ValidatorTests
{

    private readonly CreateProductCommandValidator _createProductValidator = new();

    [Fact]
    public void CreateProduct_ValidCommand_PassesValidation()
    {
        var result = _createProductValidator.Validate(new CreateProductCommand("Widget", 9.99m, 10));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateProduct_EmptyOrWhitespaceName_FailsValidation(string? name)
    {
        var result = _createProductValidator.Validate(new CreateProductCommand(name!, 9.99m, 10));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void CreateProduct_NameTooLong_FailsValidation()
    {
        var longName = new string('A', 201);
        var result = _createProductValidator.Validate(new CreateProductCommand(longName, 9.99m, 10));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void CreateProduct_Exactly200CharName_Passes()
    {
        var name = new string('A', 200);
        var result = _createProductValidator.Validate(new CreateProductCommand(name, 9.99m, 10));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProduct_NegativePrice_FailsValidation()
    {
        var result = _createProductValidator.Validate(new CreateProductCommand("Widget", -0.01m, 10));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void CreateProduct_ZeroPrice_Passes()
    {
        var result = _createProductValidator.Validate(new CreateProductCommand("Widget", 0m, 10));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProduct_NegativeStock_FailsValidation()
    {
        var result = _createProductValidator.Validate(new CreateProductCommand("Widget", 9.99m, -1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StockQuantity");
    }

    [Fact]
    public void CreateProduct_ZeroStock_Passes()
    {
        var result = _createProductValidator.Validate(new CreateProductCommand("Widget", 9.99m, 0));
        result.IsValid.Should().BeTrue();
    }

private readonly UpdateProductCommandValidator _updateProductValidator = new();

    [Fact]
    public void UpdateProduct_ValidCommand_PassesValidation()
    {
        var result = _updateProductValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "Widget", 9.99m, 5));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateProduct_EmptyId_FailsValidation()
    {
        var result = _updateProductValidator.Validate(new UpdateProductCommand(Guid.Empty, "Widget", 9.99m, 5));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void UpdateProduct_EmptyName_FailsValidation()
    {
        var result = _updateProductValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "", 9.99m, 5));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void UpdateProduct_NegativePrice_FailsValidation()
    {
        var result = _updateProductValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "Widget", -1m, 5));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void UpdateProduct_NegativeStock_FailsValidation()
    {
        var result = _updateProductValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "Widget", 9.99m, -1));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StockQuantity");
    }

private readonly DeleteProductCommandValidator _deleteProductValidator = new();

    [Fact]
    public void DeleteProduct_ValidId_PassesValidation()
    {
        var result = _deleteProductValidator.Validate(new DeleteProductCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteProduct_EmptyId_FailsValidation()
    {
        var result = _deleteProductValidator.Validate(new DeleteProductCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

private readonly GetProductByIdQueryValidator _getProductByIdValidator = new();

    [Fact]
    public void GetProductById_ValidId_PassesValidation()
    {
        var result = _getProductByIdValidator.Validate(new GetProductByIdQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetProductById_EmptyId_FailsValidation()
    {
        var result = _getProductByIdValidator.Validate(new GetProductByIdQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

private readonly GetProductsQueryValidator _getProductsValidator = new();

    [Fact]
    public void GetProducts_ValidQuery_PassesValidation()
    {
        var result = _getProductsValidator.Validate(new GetProductsQuery(null, 1, 10));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetProducts_PageZero_FailsValidation()
    {
        var result = _getProductsValidator.Validate(new GetProductsQuery(null, 0, 10));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public void GetProducts_PageSizeZero_FailsValidation()
    {
        var result = _getProductsValidator.Validate(new GetProductsQuery(null, 1, 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void GetProducts_PageSize101_FailsValidation()
    {
        var result = _getProductsValidator.Validate(new GetProductsQuery(null, 1, 101));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void GetProducts_PageSize100_PassesValidation()
    {
        var result = _getProductsValidator.Validate(new GetProductsQuery(null, 1, 100));
        result.IsValid.Should().BeTrue();
    }

private readonly CreateOrderCommandValidator _createOrderValidator = new();

    [Fact]
    public void CreateOrder_ValidCommand_PassesValidation()
    {
        var result = _createOrderValidator.Validate(new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1 }
            }));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateOrder_EmptyItems_FailsValidation()
    {
        var result = _createOrderValidator.Validate(new CreateOrderCommand(
            new List<CreateOrderItemRequest>()));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void CreateOrder_NullItems_FailsValidation()
    {
        var result = _createOrderValidator.Validate(new CreateOrderCommand(null!));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void CreateOrder_ZeroQuantity_FailsValidation()
    {
        var result = _createOrderValidator.Validate(new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 0 }
            }));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public void CreateOrder_NegativeQuantity_FailsValidation()
    {
        var result = _createOrderValidator.Validate(new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = -5 }
            }));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateOrder_EmptyProductId_FailsValidation()
    {
        var result = _createOrderValidator.Validate(new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = Guid.Empty, Quantity = 1 }
            }));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ProductId"));
    }

private readonly CancelOrderCommandValidator _cancelOrderValidator = new();

    [Fact]
    public void CancelOrder_ValidId_PassesValidation()
    {
        var result = _cancelOrderValidator.Validate(new CancelOrderCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CancelOrder_EmptyId_FailsValidation()
    {
        var result = _cancelOrderValidator.Validate(new CancelOrderCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

private readonly GetOrderByIdQueryValidator _getOrderByIdValidator = new();

    [Fact]
    public void GetOrderById_ValidId_PassesValidation()
    {
        var result = _getOrderByIdValidator.Validate(new GetOrderByIdQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetOrderById_EmptyId_FailsValidation()
    {
        var result = _getOrderByIdValidator.Validate(new GetOrderByIdQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

private readonly GetOrdersQueryValidator _getOrdersValidator = new();

    [Fact]
    public void GetOrders_ValidQuery_PassesValidation()
    {
        var result = _getOrdersValidator.Validate(new GetOrdersQuery(1, 10));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetOrders_PageZero_FailsValidation()
    {
        var result = _getOrdersValidator.Validate(new GetOrdersQuery(0, 10));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public void GetOrders_PageSizeTooLarge_FailsValidation()
    {
        var result = _getOrdersValidator.Validate(new GetOrdersQuery(1, 101));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void GetOrders_PageSize100_PassesValidation()
    {
        var result = _getOrdersValidator.Validate(new GetOrdersQuery(1, 100));
        result.IsValid.Should().BeTrue();
    }
}