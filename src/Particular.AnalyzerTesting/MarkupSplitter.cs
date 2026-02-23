#nullable enable
namespace Particular.AnalyzerTesting;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

record MarkupFile(string Filename, string Content);

static partial class MarkupSplitter
{
    public static MarkupFile[] SplitMarkup(string markupCode)
    {
        List<MarkupFile> files = [];

        var markupCodeSpan = markupCode.AsSpan();
        int pos = 0;
        int filenameCounter = 0;
        string? nextFilename = null;
        var matches = DocumentSplittingRegex().Matches(markupCode);
        foreach (Match splitMatch in matches)
        {
            if (splitMatch.Index > 0)
            {
                var beforeSlice = markupCodeSpan.Slice(pos, splitMatch.Index - pos);
                var filename = nextFilename ?? $"Test{++filenameCounter}.cs";
                files.Add(new MarkupFile(filename, beforeSlice.ToString()));
            }

            var filenameMatch = splitMatch.Groups["Filename"].Value;
            nextFilename = filenameMatch.Length > 0 ? filenameMatch : null;

            pos = splitMatch.Index + splitMatch.Length;
        }

        if (pos < markupCodeSpan.Length)
        {
            var filename = nextFilename ?? $"Test{++filenameCounter}.cs";

            files.Add(new MarkupFile(filename, markupCodeSpan.Slice(pos, markupCodeSpan.Length - pos).ToString()));
        }

        return [.. files];
    }

    [GeneratedRegex(@"(^|\r?\n-{5,}[^\r\n]*\r?\n)(// (?<Filename>[\w\.-]+\.cs)\r?\n)?", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex DocumentSplittingRegex();
}