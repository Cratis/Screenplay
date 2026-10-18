// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_PurposeCompleteness;

public class when_reading_shared_vectors : Specification
{
    [Fact]
    void should_match_every_shared_vector_and_selection()
    {
        var vectors = Vectors();
        vectors.GetProperty("cases").GetArrayLength().ShouldBeGreaterThan(10);
        foreach (var vector in vectors.GetProperty("cases").EnumerateArray())
        {
            var compilation = new ScreenplayCompiler().Compile(Source(vector));
            compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => $"{vector.GetProperty("name").GetString()}: {diagnostic.Code} {diagnostic.Message}").ShouldBeEmpty();
            compilation.Success.ShouldBeTrue();
            compilation.Diagnostics.Where(IsSelected).ShouldBeEmpty();
            foreach (var selection in new[] { "purposes", "PLAY0602", "PLAY0603", "PLAY0604", "PLAY0605", "PLAY0606", "all" })
            {
                CompletenessChecks.TryParse(selection, out var checks).ShouldBeTrue();
                var findings = ModelCompleteness.Check(compilation, checks).Where(IsSelected).ToArray();
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
        var compilation = new ScreenplayCompiler().Parse(Source(Vectors().GetProperty("cases")[0]), "model.play");
        compilation.Success.ShouldBeTrue();
        CompletenessChecks.TryParse("purposes", out var checks).ShouldBeTrue();
        var findings = ModelCompleteness.Check(compilation, checks);
        findings.ShouldNotBeEmpty();
        findings.All(finding => finding.Location.Path == "model.play").ShouldBeTrue();
    }

    static bool IsSelected(Diagnostic diagnostic) => new[] { "PLAY0602", "PLAY0603", "PLAY0604", "PLAY0605", "PLAY0606" }.Contains(diagnostic.Code);

    static string Source(JsonElement vector) => string.Join('\n', vector.GetProperty("source").EnumerateArray().Select(line => line.GetString()));

    static JsonElement Vectors([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "completeness-purposes.json")));
        return document.RootElement.Clone();
    }
}
