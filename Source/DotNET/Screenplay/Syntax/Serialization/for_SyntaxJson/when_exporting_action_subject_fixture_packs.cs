// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_exporting_action_subject_fixture_packs : Specification
{
    readonly Dictionary<string, JsonElement> _exports = [];
    ApplicationSyntax _raw;
    ApplicationSyntax _effective;
    JsonElement _before;
    string _directory;

    void Establish()
    {
        _directory = Fixtures();
        var compiled = new ScreenplayCompiler().Compile(File.ReadAllText(Path.Combine(_directory, "source.play")));
        _raw = compiled.Value!;
        _before = SyntaxJson.Serialize(_raw);
        _exports["diagnostics"] = JsonSerializer.SerializeToElement(compiled.Diagnostics.Where(Relevant).Select(diagnostic => new
        {
            code = diagnostic.Code,
            severity = diagnostic.Severity.ToString().ToLowerInvariant(),
            message = diagnostic.Message,
            location = new { line = diagnostic.Location.Line, column = diagnostic.Location.Column }
        }));
    }

    void Because()
    {
        _effective = ScreenActionSubjects.Resolve(_raw).Value!;
        _exports["raw.syntax"] = SyntaxJson.Serialize(_raw);
        _exports["effective.syntax"] = SyntaxJson.Serialize(_effective);
        _exports["context"] = JsonSerializer.SerializeToElement(new
        {
            source = "source.play",
            application = _before,
            locations = Locations(_raw, "$").OrderBy(entry => entry.Path, StringComparer.Ordinal).Select(entry => new
            {
                path = entry.Path,
                kind = entry.Node.GetType().Name,
                location = new { line = entry.Node.Location.Line, column = entry.Node.Location.Column }
            })
        });
    }

    [Fact] void should_leave_authored_syntax_unchanged() => SyntaxJson.StructurallyEqual(_raw, SyntaxJson.Deserialize(_before)).ShouldBeTrue();
    [Fact] void should_be_idempotent() => SyntaxJson.StructurallyEqual(_effective, ScreenActionSubjects.Resolve(_effective).Value!).ShouldBeTrue();

    [Fact]
    void should_match_the_native_fixture_pack()
    {
        var regenerate = Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_ACTION_SUBJECT_FIXTURES") == "1";
        foreach (var (name, value) in _exports)
        {
            var path = Path.Combine(_directory, $"native.{name}.json");
            if (regenerate) File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            else Assert.True(JsonElement.DeepEquals(value, JsonDocument.Parse(File.ReadAllText(path)).RootElement), path);
        }
        Assert.False(regenerate, "Fixture pack regenerated; review and rerun without SCREENPLAY_REGENERATE_ACTION_SUBJECT_FIXTURES");
    }

    [Fact]
    void should_agree_with_typescript_on_authored_and_effective_members()
    {
        foreach (var name in new[] { "raw.syntax", "effective.syntax" })
        {
            var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, $"typescript.{name}.json"))).RootElement;
            Compare(expected, _exports[name], "$");
        }
    }

    static bool Relevant(Diagnostic diagnostic) => diagnostic.Code is DiagnosticCodes.InvalidInteractionArgument or DiagnosticCodes.UnknownActionSubjectField or DiagnosticCodes.UnresolvedActionSubject or DiagnosticCodes.InvalidActionSubjectClause or DiagnosticCodes.InferredActionSubject or DiagnosticCodes.ChangedActionSubject or DiagnosticCodes.AmbiguousActionInputBinding or DiagnosticCodes.UnsupportedActionSubjectContext;

    static IEnumerable<(string Path, SyntaxNode Node)> Locations(SyntaxNode node, string path)
    {
        yield return (path, node);
        foreach (var member in SyntaxKinds.For(node.GetType()).Members)
        {
            var value = member.Property.GetValue(node);
            if (value is SyntaxNode child)
            {
                foreach (var entry in Locations(child, $"{path}.{member.Name}")) yield return entry;
            }
            else if (value is IEnumerable items and not string)
            {
                var index = 0;
                foreach (var item in items)
                {
                    if (item is SyntaxNode element)
                    {
                        foreach (var entry in Locations(element, $"{path}.{member.Name}[{index}]")) yield return entry;
                    }
                    index++;
                }
            }
        }
    }

    static void Compare(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(actual.ValueKind == expected.ValueKind, path);
        if (expected.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in expected.EnumerateObject())
            {
                Assert.True(actual.TryGetProperty(property.Name, out var value), $"{path}.{property.Name}");
                Compare(property.Value, value, $"{path}.{property.Name}");
            }
        }
        else if (expected.ValueKind == JsonValueKind.Array)
        {
            Assert.True(actual.GetArrayLength() == expected.GetArrayLength(), path);
            for (var index = 0; index < expected.GetArrayLength(); index++) Compare(expected[index], actual[index], $"{path}[{index}]");
        }
        else Assert.True(JsonElement.DeepEquals(expected, actual), path);
    }

    static string Fixtures([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return Path.Combine(directory!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "action-subjects");
    }
}
