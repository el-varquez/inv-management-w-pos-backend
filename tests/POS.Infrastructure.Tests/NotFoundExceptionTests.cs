using POS.Domain.Exceptions;
using Xunit;

namespace POS.Infrastructure.Tests;

public class NotFoundExceptionTests
{
    [Fact]
    public void Message_names_the_entity_it_was_given()
    {
        var id = Guid.Parse("743ca6dc-cc55-4adc-b4b7-3811d4d4c605");

        var ex = new NotFoundException("Suki", id);

        Assert.Equal("Suki with id '743ca6dc-cc55-4adc-b4b7-3811d4d4c605' was not found.", ex.Message);
    }

    [Fact]
    public void Message_never_leaks_the_entity_placeholder()
    {
        var ex = new NotFoundException("Item", Guid.NewGuid());

        Assert.DoesNotContain("(entity)", ex.Message);
        Assert.StartsWith("Item with id '", ex.Message);
    }

    [Fact]
    public void Message_closes_the_quote_around_the_id()
    {
        var id = Guid.NewGuid();

        var ex = new NotFoundException("Day", id);

        Assert.Contains($"'{id}'", ex.Message);
    }
}
