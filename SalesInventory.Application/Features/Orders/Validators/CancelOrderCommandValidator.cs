using FluentValidation;
using SalesInventory.Application.Features.Orders.Commands;

namespace SalesInventory.Application.Features.Orders.Validators;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required.");
    }
}
