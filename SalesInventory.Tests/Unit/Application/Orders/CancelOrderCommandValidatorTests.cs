using FluentAssertions;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.Validators;

namespace SalesInventory.Tests.Unit.Application.Orders;

public class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommandValidator _validator;

    public CancelOrderCommandValidatorTests()
    {
        _validator = new CancelOrderCommandValidator();
    }

    [Fact]
    public void Validate_EmptyOrderId_FailsValidation()
    {
        var command = new CancelOrderCommand(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void Validate_ValidOrderId_PassesValidation()
    {
        var command = new CancelOrderCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
