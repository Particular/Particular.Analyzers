#nullable enable
namespace Particular.AnalyzerTesting;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

record MarkupFile(string Filename, string Content);

static partial class MarkupSplitter
{
    public static MarkupFile[] SplitMarkup(string markupCode)
    {
        List<MarkupFile> files = [];

        var markupCodeSpan = markupCode.AsSpan();
        int filenameCounter = 0;
        var b = new StringBuilder();
        string? filename = null;

        foreach (var lineRange in NewLineRegex().EnumerateSplits(markupCode))
        {
            var line = markupCodeSpan[lineRange.Start.Value..lineRange.End.Value];

            if (line.StartsWith("//"))
            {
                var match = FilenameRegex().Match(line.ToString());
                if (match.Success)
                {
                    filename = match.Groups["Filename"].Value;
                    continue;
                }
            }

            if (line.StartsWith("-----"))
            {
                if (b.Length > 0)
                {
                    filename ??= $"Test{++filenameCounter}.cs";
                    files.Add(new MarkupFile(filename, b.ToString().TrimEnd()));
                    filename = null;
                    _ = b.Clear();
                }

                continue;
            }

            b.AppendLine(line.ToString());
        }

        if (b.Length > 0)
        {
            filename ??= $"Test{++filenameCounter}.cs";
            files.Add(new MarkupFile(filename, b.ToString().TrimEnd()));
        }

        return [.. files];
    }

    [GeneratedRegex(@"^// (?<Filename>[\w\.-]+\.cs)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex FilenameRegex();

    [GeneratedRegex("\r?\n")]
    private static partial Regex NewLineRegex();
}