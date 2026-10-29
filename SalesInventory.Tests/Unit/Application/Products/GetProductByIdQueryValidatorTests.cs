using FluentAssertions;
using SalesInventory.Application.Features.Products.Queries;
using SalesInventory.Application.Features.Products.Validators;

namespace SalesInventory.Tests.Unit.Application.Products;

public class GetProductByIdQueryValidatorTests
{
    private readonly GetProductByIdQueryValidator _validator;

    public GetProductByIdQueryValidatorTests()
    {
        _validator = new GetProductByIdQueryValidator();
    }

    [Fact]
    public void Validate_EmptyId_FailsValidation()
    {
        var query = new GetProductByIdQuery(Guid.Empty);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_ValidId_PassesValidation()
    {
        var query = new GetProductByIdQuery(Guid.NewGuid());

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
