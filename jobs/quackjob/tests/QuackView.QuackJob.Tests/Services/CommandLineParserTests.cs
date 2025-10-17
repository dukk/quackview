using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Tests.Services;

[TestClass]
public sealed class CommandLineParserTests
{
    [TestMethod]
    public void ParseArgs_ValidArguments_ReturnsCorrectDictionary()
    {
        // Arrange
        var parser = new CommandLineParser();
        var args = new string[]
        {
            "--key1=value1",
            "-key2='value with spaces'",
            "/key3=\"another value\"",
            "--flag"
        };

        // Act
        var result = parser.ParseArgs(args);

        // Assert
        Assert.AreEqual(4, result.Count);
        Assert.AreEqual("value1", result["key1"]);
        Assert.AreEqual("value with spaces", result["key2"]);
        Assert.AreEqual("another value", result["key3"]);
        Assert.AreEqual("true", result["flag"]);
    }

    [TestMethod]
    public void ParseArgs_EmptyArguments_ReturnsEmptyDictionary()
    {
        // Arrange
        var parser = new CommandLineParser();
        var args = Array.Empty<string>();

        // Act
        var result = parser.ParseArgs(args);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}