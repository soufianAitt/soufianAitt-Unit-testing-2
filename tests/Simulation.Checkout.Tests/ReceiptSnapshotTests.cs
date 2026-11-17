using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using static VerifyXunit.Verifier;

namespace Simulation.Checkout.Tests;

// Task 4 - a snapshot test saves the whole receipt in a text file (.verified.txt)
// and compares it with the receipt on every run.
public class ReceiptSnapshotTests
{
    // 4.1  Example C: 3 x 500 = 1500, discount capped at 100, total 1400
    [Fact]
    public async Task CheckoutAsync_WhenExampleC_ShouldMatchReceiptSnapshot()
    {
        // Arrange - everything is fixed, nothing random
        var inventory = new Mock<IInventoryService>();
        inventory
            .Setup(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(
            new Mock<IOrderRepository>().Object,
            inventory.Object,
            new Mock<INotificationService>().Object);

        var order = new Order
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CustomerName = "Sara Ahmed",
            CustomerEmail = "sara@example.com",
            Items = new[]
            {
                new OrderItem
                {
                    ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ProductName = "Headphones",
                    UnitPrice = 500m,
                    Quantity = 3
                }
            }
        };

        // Act
        var receipt = await service.CheckoutAsync(order);

        // Assert
        // CreatedAt changes every run, so we leave it out of the snapshot.
        // OrderId is fixed above, but we ignore it too to be safe.
        await Verify(receipt)
            .IgnoreMember("CreatedAt")
            .IgnoreMember("OrderId");
    }
}
