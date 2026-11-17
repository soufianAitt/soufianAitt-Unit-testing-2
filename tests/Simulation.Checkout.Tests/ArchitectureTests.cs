using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Infrastructure;

namespace Simulation.Checkout.Tests;

// Task 5 - tests that check the project structure (who is allowed to use whom).
public class ArchitectureTests
{
    // One assembly (project) for each layer.
    private static readonly Assembly DomainAssembly = typeof(Order).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(CheckoutService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(InMemoryOrderRepository).Assembly;

    private const string ApplicationNamespace = "Simulation.Checkout.Application";
    private const string InfrastructureNamespace = "Simulation.Checkout.Infrastructure";

    // 5.1
    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrInfrastructure()
    {
        // Arrange
        var rule = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace);

        // Act
        var result = rule.GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    // 5.2
    [Fact]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        // Arrange
        var rule = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace);

        // Act
        var result = rule.GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    // 5.3
    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        // Arrange
        var rule = Types.InAssemblies(new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly })
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I");

        // Act
        var result = rule.GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    // 5.4
    [Fact]
    public void OrderRepositoryImplementations_ShouldEndWithRepository()
    {
        // Arrange
        var rule = Types.InAssemblies(new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly })
            .That()
            .ImplementInterface(typeof(IOrderRepository))
            .Should()
            .HaveNameEndingWith("Repository");

        // Act
        var result = rule.GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
}
