using AutoMapper;
using MediatR;
using SalesInventory.Application.Exceptions;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Interfaces;
using SalesInventory.Domain.Entities;
using SalesInventory.Domain.Enums;

namespace SalesInventory.Application.Features.Orders.Commands;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Items", "An order must contain at least one item.") });

        var consolidatedItems = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new CreateOrderItemRequest
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderBy(i => i.ProductId)
            .ToList();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var productIds = consolidatedItems.Select(i => i.ProductId).ToList();
            var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
            var productDict = products.ToDictionary(p => p.Id);

            foreach (var item in consolidatedItems)
            {
                if (!productDict.TryGetValue(item.ProductId, out var product))
                    throw new NotFoundException(nameof(Product), item.ProductId);

                if (product.StockQuantity < item.Quantity)
                    throw new InsufficientStockException(product.Name, item.Quantity, product.StockQuantity);
            }

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var item in consolidatedItems)
            {
                var product = productDict[item.ProductId];
                product.StockQuantity -= item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
                _productRepository.Update(product);

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };

                orderItems.Add(orderItem);
                totalAmount += orderItem.Quantity * orderItem.UnitPrice;
            }

            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Status = OrderStatus.Completed,
                Items = orderItems
            };

            foreach (var item in orderItems)
                item.OrderId = order.Id;

            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return _mapper.Map<OrderDto>(order);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

}
