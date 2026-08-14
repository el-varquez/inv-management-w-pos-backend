using POS.Application.Common;
using Xunit;

namespace POS.Infrastructure.Tests;

public class ItemCodeGeneratorTests
{
    [Fact]
    public void Empty_catalog_starts_at_00001()
        => Assert.Equal("00001", ItemCodeGenerator.Next([]));

    [Fact]
    public void Increments_past_highest_numeric_code()
        => Assert.Equal("00010", ItemCodeGenerator.Next(["00001", "00009", "00003"]));

    [Fact]
    public void Ignores_non_numeric_codes()
        => Assert.Equal("00005", ItemCodeGenerator.Next(["Coke 1L", "00004", "KOPIKO-TWIN-10"]));

    [Fact]
    public void Counts_unpadded_numeric_codes()
        => Assert.Equal("00008", ItemCodeGenerator.Next(["7"]));

    [Fact]
    public void Ignores_numeric_codes_longer_than_nine_digits()
        => Assert.Equal("00001", ItemCodeGenerator.Next(["4800016641234"]));

    [Fact]
    public void Grows_past_five_digits_without_truncating()
        => Assert.Equal("100000", ItemCodeGenerator.Next(["99999"]));

    [Fact]
    public void Trims_and_skips_blank_codes()
        => Assert.Equal("00003", ItemCodeGenerator.Next(["  00002  ", "   ", ""]));
}
