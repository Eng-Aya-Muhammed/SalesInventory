using FluentAssertions;
using SalesInventory.Application.Features.Orders.Queries;
using SalesInventory.Application.Features.Orders.Validators;

namespace SalesInventory.Tests.Unit.Application.Orders;

public class GetOrderByIdQueryValidatorTests
{
    private readonly GetOrderByIdQueryValidator _validator;

    public GetOrderByIdQueryValidatorTests()
    {
        _validator = new GetOrderByIdQueryValidator();
    }

    [Fact]
    public void Validate_EmptyId_FailsValidation()
    {
        var query = new GetOrderByIdQuery(Guid.Empty);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_ValidId_PassesValidation()
    {
        var query = new GetOrderByIdQuery(Guid.NewGuid());

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
