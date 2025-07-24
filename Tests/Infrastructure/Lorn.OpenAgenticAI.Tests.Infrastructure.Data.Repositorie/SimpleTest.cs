using Xunit;

namespace Lorn.OpenAgenticAI.Tests.Infrastructure.Data.Repositorie;

public class SimpleTest
{
    [Fact]
    public void Simple_Test_Should_Pass()
    {
        // Arrange
        var expected = 1;

        // Act
        var actual = 1;

        // Assert
        Assert.Equal(expected, actual);
    }
}
