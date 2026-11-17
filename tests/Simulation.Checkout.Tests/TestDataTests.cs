using AutoFixture;
using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Tests.TestData;

namespace Simulation.Checkout.Tests;

// Task 3 - test data made by AutoFixture and Bogus.
public class TestDataTests
{
    private readonly CheckoutService _service;

    public TestDataTests()
    {
        // Mocks for the 3 interfaces. Everything is in stock.
        var inventory = new Mock<IInventoryService>();
        inventory
            .Setup(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new CheckoutService(
            new Mock<IOrderRepository>().Object,
            inventory.Object,
            new Mock<INotificationService>().Object);
    }

    // 3.1
    [Fact]
    public async Task CheckoutAsync_WhenOrderIsCreatedByAutoFixture_ShouldSucceed()
    {
        // Arrange
        var fixture = new Fixture();

        // AutoFixture fills properties with random values. That could give us
        // quantity 0 or a negative price, so we tell it to only create valid items.
        fixture.Customize<OrderItem>(c => c
            .With(item => item.Quantity, () => Random.Shared.Next(1, 10))
            .With(item => item.UnitPrice, () => (decimal)Random.Shared.Next(0, 500)));

        var items = fixture.CreateMany<OrderItem>(3).ToList();
        var order = fixture.Build<Order>()
            .With(o => o.Items, items)
            .Create();

        // Act
        var receipt = await _service.CheckoutAsync(order);

        // Assert
        receipt.OrderId.Should().Be(order.Id);
        receipt.Items.Should().HaveCount(3);
        receipt.Total.Should().BeGreaterThanOrEqualTo(0m);
    }

    // 3.2
    [Fact]
    public async Task CheckoutAsync_WhenCustomerIsCreatedByBogus_ShouldPutSameNameAndEmailOnReceipt()
    {
        // Arrange
        var customer = CustomerFaker.CreateCustomer(seed: 1234); // fixed seed = same customer every run
        var order = new Order
        {
            CustomerName = customer.Name,
            CustomerEmail = customer.Email,
            Items = new[] { TestOrders.CreateItem("Notebook", 50m, 2) }
        };

        // Act
        var receipt = await _service.CheckoutAsync(order);

        // Assert
        receipt.CustomerName.Should().Be(customer.Name);
        receipt.CustomerEmail.Should().Be(customer.Email);
    }
}
