// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_DataBindingCompleteness;

public class when_reading_shared_vectors : Specification
{
    [Fact]
    void should_match_every_shared_vector_and_selection()
    {
        var vectors = Vectors();
        vectors.GetProperty("cases").GetArrayLength().ShouldBeGreaterThan(20);
        var compiled = vectors.GetProperty("cases").EnumerateArray()
            .Select(vector => (Vector: vector, Compilation: new ScreenplayCompiler().Compile(Source(vectors, vector)))).ToArray();
        compiled.SelectMany(entry => entry.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => $"{entry.Vector.GetProperty("name").GetString()}: {diagnostic.Code} {diagnostic.Message}")).ShouldBeEmpty();
        foreach (var (vector, compilation) in compiled)
        {
            compilation.Success.ShouldBeTrue();
            compilation.Diagnostics.Where(IsBinding).ShouldBeEmpty();
            foreach (var selection in new[] { "data-bindings", "PLAY0530", "PLAY0531", "all" })
            {
                CompletenessChecks.TryParse(selection, out var checks).ShouldBeTrue();
                var findings = ModelCompleteness.Check(compilation, checks).Where(IsBinding).ToArray();
                var expected = vector.GetProperty("diagnostics").EnumerateArray().ToArray();
                findings.Length.ShouldEqual(expected.Length);
                foreach (var (finding, diagnostic) in findings.Zip(expected))
                {
                    finding.Code.ShouldEqual(diagnostic.GetProperty("code").GetString());
                    finding.Severity.ToString().ToLowerInvariant().ShouldEqual(diagnostic.GetProperty("severity").GetString());
                    finding.Message.ShouldEqual(diagnostic.GetProperty("message").GetString());
                    finding.Location.ShouldEqual(new SourceLocation(diagnostic.GetProperty("location").GetProperty("line").GetInt32(), diagnostic.GetProperty("location").GetProperty("column").GetInt32()));
                }
            }
            ModelCompleteness.Check(compilation, CompletenessChecks.None).ShouldBeEmpty();
        }
    }

    [Fact]
    void should_preserve_the_source_path()
    {
        var vectors = Vectors();
        var compilation = new ScreenplayCompiler().Parse(Source(vectors, vectors.GetProperty("cases")[0]), "view.play");
        compilation.Success.ShouldBeTrue();
        ModelCompleteness.Check(compilation, new([CompletenessCheck.DataBindings])).Single().Location.Path.ShouldEqual("view.play");
    }

    static bool IsBinding(Diagnostic diagnostic) => diagnostic.Code == DiagnosticCodes.ConflictingScreenDataBinding || diagnostic.Code == DiagnosticCodes.ScreenDataQueryMismatch;

    static string Source(JsonElement vectors, JsonElement vector) => vector.TryGetProperty("source", out var source)
        ? string.Join('\n', source.EnumerateArray().Select(line => line.GetString()))
        : string.Join('\n', vectors.GetProperty("declarations").EnumerateArray().Select(line => line.GetString())
            .Concat(vector.GetProperty("directives").EnumerateArray().Select(line => "        " + line.GetString())));

    static JsonElement Vectors([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "completeness-data-bindings.json")));
        return document.RootElement.Clone();
    }
}
