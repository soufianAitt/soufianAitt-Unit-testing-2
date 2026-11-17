using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

// Small helper methods so every test can build an order in one line.
public static class TestOrders
{
    public static OrderItem CreateItem(string name, decimal unitPrice, int quantity)
    {
        return new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductName = name,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }

    // "params" lets us write CreateOrder(item1, item2) or CreateOrder() for an empty order.
    public static Order CreateOrder(params OrderItem[] items)
    {
        return new Order
        {
            CustomerName = "Test Customer",
            CustomerEmail = "customer@test.com",
            Items = items
        };
    }
}
