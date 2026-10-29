using FluentValidation;
using SalesInventory.Application.Features.Orders.Queries;

namespace SalesInventory.Application.Features.Orders.Validators;

public class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Order ID is required.");
    }
}
