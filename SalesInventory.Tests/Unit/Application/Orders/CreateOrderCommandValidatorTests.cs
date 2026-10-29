using FluentAssertions;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Orders.Validators;

namespace SalesInventory.Tests.Unit.Application.Orders;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator;

    public CreateOrderCommandValidatorTests()
    {
        _validator = new CreateOrderCommandValidator();
    }

    [Fact]
    public void Validate_EmptyItems_FailsValidation()
    {
        var command = new CreateOrderCommand(new List<CreateOrderItemRequest>());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_ZeroOrNegativeQuantity_FailsValidation()
    {
        var command = new CreateOrderCommand(new List<CreateOrderItemRequest>
        {
            new() { ProductId = Guid.NewGuid(), Quantity = 0 }
        });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public void Validate_EmptyProductId_FailsValidation()
    {
        var command = new CreateOrderCommand(new List<CreateOrderItemRequest>
        {
            new() { ProductId = Guid.Empty, Quantity = 5 }
        });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ProductId"));
    }

    [Fact]
    public void Validate_ValidItems_PassesValidation()
    {
        var command = new CreateOrderCommand(new List<CreateOrderItemRequest>
        {
            new() { ProductId = Guid.NewGuid(), Quantity = 3 }
        });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
