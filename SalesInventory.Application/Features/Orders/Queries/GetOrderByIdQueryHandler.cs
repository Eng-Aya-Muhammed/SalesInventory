using AutoMapper;
using MediatR;
using SalesInventory.Application.Exceptions;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Interfaces;
using SalesInventory.Domain.Entities;

namespace SalesInventory.Application.Features.Orders.Queries;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesOrder), request.Id);

        return _mapper.Map<OrderDto>(order);
    }

}
