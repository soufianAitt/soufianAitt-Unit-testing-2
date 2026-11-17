using Bogus;

namespace Simulation.Checkout.Tests.TestData;

// A tiny customer class, only used by the tests.
public class TestCustomer
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

// Bogus creates realistic fake data (names, emails).
// UseSeed makes it give the SAME values on every run, so tests never "randomly" fail.
public static class CustomerFaker
{
    public static TestCustomer CreateCustomer(int seed = 1234)
    {
        var faker = new Faker<TestCustomer>()
            .UseSeed(seed)
            .RuleFor(c => c.Name, f => f.Name.FullName())
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.Name));

        return faker.Generate();
    }
}
