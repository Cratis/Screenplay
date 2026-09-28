// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.CanonicalVectors.for_RegisterProjectCorpus;

public class when_annotating_every_canonical_source_line
{
    [Fact]
    public void should_preserve_each_unique_comment_once_and_print_stably()
    {
        var compiler = new ScreenplayCompiler();
        var printer = new ScreenplayPrinter();
        var documents = new[] { RegisterProjectCorpus.LegacyV1, RegisterProjectCorpus.V2 }
            .SelectMany(corpus => corpus.SourceForms.SelectMany(form => form.Documents))
            .Select(document => (document.DisplayPath, document.Text)).ToList();
        using (var stream = typeof(when_annotating_every_canonical_source_line).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalVectors.Samples.invoicing.play")!)
        using (var reader = new StreamReader(stream))
        {
            documents.Add(("invoicing.play", reader.ReadToEnd()));
        }

        Assert.NotEmpty(documents);
        var nextId = 0;
        foreach (var document in documents)
        {
            var markers = new List<string>();
            var inFence = false;
            var source = string.Join('\n', document.Text.Split('\n').Select(line =>
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    inFence = !inFence;
                    return line;
                }

                if (inFence || trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    return line;
                }

                var marker = $"// __anchor_{nextId++:D4}";
                markers.Add(marker);
                return line.TrimEnd('\r') + " " + marker;
            }));
            Assert.NotEmpty(markers);
            var parsed = compiler.Compile(source);
            Assert.True(parsed.Success, $"{document.DisplayPath}: {string.Join("; ", parsed.Diagnostics)}");
            var printed = printer.Print(parsed.Value!);
            var reparsed = compiler.Compile(printed);
            Assert.True(reparsed.Success, $"{document.DisplayPath}: {string.Join("; ", reparsed.Diagnostics)}");
            var printedAgain = printer.Print(reparsed.Value!);
            Assert.Equal(printed, printedAgain);
            foreach (var marker in markers)
            {
                Assert.Equal(1, printed.Split(marker, StringSplitOptions.None).Length - 1);
            }
        }
    }
}
