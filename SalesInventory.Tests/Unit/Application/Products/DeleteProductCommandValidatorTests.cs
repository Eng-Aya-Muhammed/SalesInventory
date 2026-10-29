using FluentAssertions;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.Validators;

namespace SalesInventory.Tests.Unit.Application.Products;

public class DeleteProductCommandValidatorTests
{
    private readonly DeleteProductCommandValidator _validator;

    public DeleteProductCommandValidatorTests()
    {
        _validator = new DeleteProductCommandValidator();
    }

    [Fact]
    public void Validate_EmptyId_FailsValidation()
    {
        var command = new DeleteProductCommand(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_ValidId_PassesValidation()
    {
        var command = new DeleteProductCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
