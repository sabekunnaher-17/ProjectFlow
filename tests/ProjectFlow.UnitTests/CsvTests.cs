using ProjectFlow.Core.Common;

namespace ProjectFlow.UnitTests;

public sealed class CsvTests
{
    [Theory]
    [InlineData("=SUM(A1)")]
    [InlineData(" +cmd")]
    [InlineData("\t@formula")]
    [InlineData("-2+3")]
    public void FormulaCellsAreNeutralized(string input) => Assert.StartsWith("\"'", Csv.Cell(input));
    [Fact] public void QuotesAndNewlinesStayInsideTheQuotedCell() => Assert.Equal("\"a,\"\"b\"\"\nline\"", Csv.Cell("a,\"b\"\nline"));
}
