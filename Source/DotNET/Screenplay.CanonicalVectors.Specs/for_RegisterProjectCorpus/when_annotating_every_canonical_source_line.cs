// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.CanonicalVectors.for_RegisterProjectCorpus;

public class when_annotating_every_canonical_source_line : Specification
{
    record Marker(string Text, string Kind);
    record Result(string Path, Marker[] Markers, bool Compiled, string Diagnostics, string Printed, bool Recompiled, string Reprint);

    List<Result> _results = [];

    void Because()
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

        documents.Add(("repeated-files.play", """
            concept Id : Uuid
              file A.cs
              file B.cs

            type Entry
              file A.cs
              file B.cs
              id String

            trigger Started
              file A.cs
              file B.cs

            behavior Act
              file A.cs
              file B.cs

            module Sales
              feature Orders
                slice StateChange Place
                  file A.cs
                  file B.cs
                  event Created
                    file A.cs
                    file B.cs
                  readmodel Order
                    file A.cs
                    file B.cs
                  projection Orders => Order
                    file A.cs
                    file B.cs
                    from Created
                      id = id
                  specification Works
                    file A.cs
                    file B.cs
                  screen Main
                    file A.cs
                    file B.cs
                slice StateView View
                  readmodel Summary
                  reducer Summarize => Summary
                    on Created
                      file A.cs
                      file B.cs
                slice Automation Sync
                  reaction Update
                    when Started
                      file A.cs
                      file B.cs
            """));

        var nextId = 0;
        foreach (var document in documents)
        {
            var markers = new List<Marker>();
            var inFence = false;
            var previousKind = string.Empty;
            var previousIndent = 0;
            var sourceLines = document.Text.Split('\n');
            var source = string.Join('\n', sourceLines.Select((line, index) =>
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
                var kind = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                var indent = line.Length - line.TrimStart().Length;
                if (indent > previousIndent && (previousKind == "authorize" || previousKind == "require"))
                {
                    // Continued expressions collapse into their owning directive when printed.
                    kind = previousKind;
                }
                else if (kind != "description" && !trimmed.Contains(' ') &&
                    sourceLines.Skip(index + 1).FirstOrDefault(next => next.Trim().Length > 0)?.Trim() == "```")
                {
                    // Legacy language-only lines become tagged fences.
                    kind = $"```{kind}";
                }

                markers.Add(new(marker, kind));
                previousKind = kind;
                previousIndent = indent;
                return line.TrimEnd('\r') + " " + marker;
            }));
            var parsed = compiler.Compile(source);
            var printed = parsed.Success ? printer.Print(parsed.Value!) : string.Empty;
            var reparsed = parsed.Success ? compiler.Compile(printed) : null;
            _results.Add(new(
                document.DisplayPath,
                [.. markers],
                parsed.Success,
                string.Join("; ", parsed.Diagnostics),
                printed,
                reparsed?.Success == true,
                reparsed?.Success == true ? printer.Print(reparsed.Value!) : string.Empty));
        }
    }

    [Fact] void should_compile_every_document() => _results.Where(result => !result.Compiled).Select(result => $"{result.Path}: {result.Diagnostics}").ShouldBeEmpty();
    [Fact] void should_recompile_every_document() => _results.Where(result => !result.Recompiled).Select(result => result.Path).ShouldBeEmpty();
    [Fact] void should_print_stably() => _results.Where(result => result.Printed != result.Reprint).Select(result => result.Path).ShouldBeEmpty();
    [Fact] void should_annotate_every_document() => _results.Where(result => result.Markers.Length == 0).Select(result => result.Path).ShouldBeEmpty();

    [Fact]
    void should_preserve_each_marker_once()
    {
        _results.SelectMany(result => result.Markers.Where(marker => result.Printed.Split(marker.Text, StringSplitOptions.None).Length != 2)
            .Select(marker => $"{result.Path}: {marker.Text}")).ShouldBeEmpty();
    }

    [Fact]
    void should_keep_each_marker_on_its_own_line()
    {
        _results.SelectMany(result => result.Printed.Split('\n')
            .Where(line => line.Split("__anchor_", StringSplitOptions.None).Length > 2)
            .Select(line => $"{result.Path}: {line.Trim()}")).ShouldBeEmpty();
    }

    [Fact]
    void should_keep_each_marker_on_the_same_kind_of_line()
    {
        _results.SelectMany(result => result.Markers.Select(marker =>
        {
            var lines = result.Printed.Split('\n');
            var index = Array.FindIndex(lines, line => line.Contains(marker.Text, StringComparison.Ordinal));
            if (index < 0)
            {
                return $"{result.Path}: missing {marker.Text}";
            }

            var line = lines[index].Trim();
            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                // A canonicalized-away line retains its comment immediately above its replacement.
                index = Array.FindIndex(lines, index + 1, line => line.Trim().Length > 0 && !line.TrimStart().StartsWith("//", StringComparison.Ordinal));
                if (index < 0)
                {
                    return $"{result.Path}: {marker.Text} has no printed owner";
                }

                line = lines[index].Trim();
            }

            var kind = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            return kind == marker.Kind ? null : $"{result.Path}: {marker.Text} moved from {marker.Kind} to {kind}";
        }).OfType<string>()).ShouldBeEmpty();
    }
}
