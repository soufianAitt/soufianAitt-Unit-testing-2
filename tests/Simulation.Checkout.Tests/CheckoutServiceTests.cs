using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using Simulation.Checkout.Tests.TestData;

namespace Simulation.Checkout.Tests;

// Tasks 1 and 2.
// xUnit creates a NEW instance of this class for every test,
// so the constructor gives each test fresh mocks.
public class CheckoutServiceTests
{
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly CheckoutService _service;

    public CheckoutServiceTests()
    {
        // By default every product is in stock.
        _inventory
            .Setup(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new CheckoutService(_repository.Object, _inventory.Object, _notifications.Object);
    }

    // ---------------------------------------------------------------
    // TASK 1 - Business rules
    // ---------------------------------------------------------------

    // 1.1  BR-01
    [Fact]
    public async Task CheckoutAsync_WhenOrderIsEmpty_ShouldThrowEmptyOrderException()
    {
        // Arrange
        var order = TestOrders.CreateOrder(); // no items

        // Act
        Func<Task> act = () => _service.CheckoutAsync(order);

        // Assert
        await act.Should().ThrowAsync<EmptyOrderException>();
    }

    // 1.2  BR-02
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CheckoutAsync_WhenQuantityIsZeroOrNegative_ShouldThrowInvalidQuantityException(int quantity)
    {
        // Arrange
        var order = TestOrders.CreateOrder(TestOrders.CreateItem("Laptop", 100m, quantity));

        // Act
        Func<Task> act = () => _service.CheckoutAsync(order);

        // Assert
        await act.Should().ThrowAsync<InvalidQuantityException>().WithMessage("*Laptop*");
    }

    // 1.3  BR-03
    [Fact]
    public async Task CheckoutAsync_WhenPriceIsNegative_ShouldThrowInvalidPriceException()
    {
        // Arrange
        var order = TestOrders.CreateOrder(TestOrders.CreateItem("Mouse", -0.01m, 1));

        // Act
        Func<Task> act = () => _service.CheckoutAsync(order);

        // Assert
        await act.Should().ThrowAsync<InvalidPriceException>();
    }

    // 1.4  BR-03 (price 0 is allowed)
    [Fact]
    public async Task CheckoutAsync_WhenPriceIsZero_ShouldSucceedWithZeroTotal()
    {
        // Arrange
        var order = TestOrders.CreateOrder(TestOrders.CreateItem("Free Sticker", 0m, 1));

        // Act
        var receipt = await _service.CheckoutAsync(order);

        // Assert
        receipt.Should().NotBeNull();
        receipt.Total.Should().Be(0m);
    }

    // 1.5  BR-06 (boundaries: just below, exactly on, and above the threshold)
    // C# attributes cannot hold decimal values, so we use double and convert to decimal.
    [Theory]
    [InlineData(999.99, 0)]
    [InlineData(1000, 100)]
    [InlineData(1500, 100)]
    public async Task CheckoutAsync_WhenSubtotalChanges_ShouldApplyCorrectDiscount(double subtotal, double expectedDiscount)
    {
        // Arrange
        var order = TestOrders.CreateOrder(TestOrders.CreateItem("Phone", (decimal)subtotal, 1));

        // Act
        var receipt = await _service.CheckoutAsync(order);

        // Assert
        receipt.Discount.Should().Be((decimal)expectedDiscount);
    }

    // 1.6  BR-05, BR-06, BR-07 (Examples A and C from the assignment)
    public static TheoryData<string, Order, decimal, decimal, decimal> ExampleOrders => new()
    {
        // Example A: 2 x 150 + 1 x 400 = 700, no discount
        {
            "Example A",
            TestOrders.CreateOrder(
                TestOrders.CreateItem("Book", 150m, 2),
                TestOrders.CreateItem("Bag", 400m, 1)),
            700m, 0m, 700m
        },
        // Example C: 3 x 500 = 1500, discount is capped at 100
        {
            "Example C",
            TestOrders.CreateOrder(TestOrders.CreateItem("Headphones", 500m, 3)),
            1500m, 100m, 1400m
        }
    };

    [Theory]
    [MemberData(nameof(ExampleOrders))]
    public async Task CheckoutAsync_WhenOrderIsValid_ShouldCalculateSubtotalDiscountAndTotal(
        string example, Order order, decimal expectedSubtotal, decimal expectedDiscount, decimal expectedTotal)
    {
        // Arrange (the order comes from ExampleOrders above)

        // Act
        var receipt = await _service.CheckoutAsync(order);

        // Assert
        receipt.Subtotal.Should().Be(expectedSubtotal, example);
        receipt.Discount.Should().Be(expectedDiscount, example);
        receipt.Total.Should().Be(expectedTotal, example);
    }

    // ---------------------------------------------------------------
    // TASK 2 - Mocks (Moq)
    // ---------------------------------------------------------------

    // 2.1  BR-08
    [Fact]
    public async Task CheckoutAsync_WhenAllItemsInStock_ShouldSaveOrderAndSendConfirmationOnce()
    {
        // Arrange
        var order = TestOrders.CreateOrder(TestOrders.CreateItem("Keyboard", 200m, 2));

        // Act
        var receipt = await _service.CheckoutAsync(order);

        // Assert
        receipt.Total.Should().Be(400m);
        _repository.Verify(r => r.SaveAsync(order, It.IsAny<CancellationToken>()), Times.Once());
        _notifications.Verify(n => n.SendOrderConfirmationAsync(receipt, It.IsAny<CancellationToken>()), Times.Once());
    }

    // 2.2  BR-04 + BR-09
    [Fact]
    public async Task CheckoutAsync_WhenOneItemIsOutOfStock_ShouldThrowAndNotSaveOrSend()
    {
        // Arrange
        var inStock = TestOrders.CreateItem("Pen", 10m, 1);
        var outOfStock = TestOrders.CreateItem("Monitor", 3000m, 1);
        var order = TestOrders.CreateOrder(inStock, outOfStock);

        // This more specific setup says: the monitor is NOT available.
        _inventory
            .Setup(i => i.IsAvailableAsync(outOfStock.ProductId, outOfStock.Quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = () => _service.CheckoutAsync(order);

        // Assert
        await act.Should().ThrowAsync<InsufficientInventoryException>();
        _repository.Verify(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never());
        _notifications.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    // 2.3  BR-09 (validation fails BEFORE the inventory is even checked)
    [Fact]
    public async Task CheckoutAsync_WhenValidationFails_ShouldNotCheckInventorySaveOrSend()
    {
        // Arrange
        var order = TestOrders.CreateOrder(TestOrders.CreateItem("Desk", 500m, 0)); // quantity 0 = invalid

        // Act
        Func<Task> act = () => _service.CheckoutAsync(order);

        // Assert
        await act.Should().ThrowAsync<InvalidQuantityException>();
        _inventory.Verify(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
        _repository.Verify(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never());
        _notifications.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}
