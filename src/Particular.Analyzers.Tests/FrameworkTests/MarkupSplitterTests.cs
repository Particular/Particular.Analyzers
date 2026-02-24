namespace Particular.Analyzers.Tests;

using AnalyzerTesting;
using NUnit.Framework;

public class MarkupSplitterTests
{
    [Test]
    public void TwoFiles()
    {
        var code = """
                   first
                   -----
                   second
                   """;

        var expected = new MarkupFile[]
        {
            new("Test1.cs", "first"),
            new("Test2.cs", "second")
        };

        var actual = MarkupSplitter.SplitMarkup(code);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void WithFilenames()
    {
        var code = """
                   // TheFirst.cs
                   first
                   -----
                   // TheSecond.cs
                   second
                   """;

        var expected = new MarkupFile[]
        {
            new("TheFirst.cs", "first"),
            new("TheSecond.cs", "second")
        };

        var actual = MarkupSplitter.SplitMarkup(code);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ThreeFilesMixed()
    {
        var code = """
                   // TheFirst.cs
                   first
                   ----------------
                   second
                   ----- garbage bug allowed#######
                   // TheLast.cs
                   last
                   """;

        var expected = new MarkupFile[]
        {
            new("TheFirst.cs", "first"),
            new("Test1.cs", "second"),
            new("TheLast.cs", "last")
        };

        var actual = MarkupSplitter.SplitMarkup(code);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void StartsAndEndsWithSeparator()
    {
        var code = """
                   ----- Using it as a comment
                   first
                   -----
                   second
                   ----- This is the end
                   """;

        var expected = new MarkupFile[]
        {
            new("Test1.cs", "first"),
            new("Test2.cs", "second")
        };

        var actual = MarkupSplitter.SplitMarkup(code);

        Assert.That(actual, Is.EqualTo(expected));
    }
}