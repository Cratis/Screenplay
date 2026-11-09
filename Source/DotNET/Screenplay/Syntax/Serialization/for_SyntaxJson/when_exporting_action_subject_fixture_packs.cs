// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_exporting_action_subject_fixture_packs : Specification
{
    static readonly JsonSerializerOptions _indented = new() { WriteIndented = true };
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
        _exports["manifest"] = JsonSerializer.SerializeToElement(new
        {
            protocol = "Cratis.ActionSubjects/1",
            authored = "source.play",
            rawSyntaxJson = "native.raw.syntax.json",
            effectiveSyntaxJson = "native.effective.syntax.json",
            diagnostics = "native.diagnostics.json",
            context = "native.context.json",
            cases = _raw.Modules.First().Features.First().Slices.First().Screens.Select((screen, index) => new { name = screen.Name, focusPath = $"$.modules[0].features[0].slices[0].screens[{index}]" })
        });
        _exports["context"] = JsonSerializer.SerializeToElement(new
        {
            source = "source.play",
            application = _before,
            subjects = Subjects(_before, "$", new(null, null, [])).ToArray(),
            locations = Locations(_raw, "$").OrderBy(entry => entry.Path, StringComparer.Ordinal).Select(entry => new
            {
                path = entry.Path,
                kind = entry.Node.GetType().Name,
                location = new { line = entry.Node.Location.Line, column = entry.Node.Location.Column }
            })
        });
    }

    [Theory]
    [InlineData("InferredItem", "item.rowId")]
    [InlineData("SameName", "item.itemId")]
    [InlineData("CaseMismatch", "item.rowId")]
    [InlineData("QualifiedDeclarations", "item.rowId")]
    [InlineData("SlotScope", "item.rowId")]
    [InlineData("OutletScope", "item.itemId")]
    void should_resolve_declared_identity_in_each_scope(string screen, string binding)
    {
        var action = Actions(screen).Single();
        action.Subject.ShouldEqual(ScreenActionSubjectKind.Item);
        action.Inputs.Single(input => input.Name == "itemId").Binding.ShouldEqual(binding);
    }

    [Theory]
    [InlineData("ForeignKey")]
    [InlineData("OptionalIdentifier")]
    [InlineData("GeneratedIdentifier")]
    [InlineData("PrimitiveIdentifier")]
    [InlineData("AmbiguousInferred")]
    [InlineData("MissingKey")]
    [InlineData("UnresolvedDeclaration")]
    void should_not_invent_identity(string screen)
    {
        var action = Actions(screen).Single();
        action.Subject.ShouldBeNull();
        action.Inputs.ShouldBeEmpty();
    }

    IEnumerable<ScreenActionSyntax> Actions(string name)
    {
        var walker = new Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.given.a_counting_walker();
        walker.VisitScreen(_effective.Modules.First().Features.First().Slices.First().Screens.Single(screen => screen.Name == name));
        return walker.Nodes.OfType<ScreenActionSyntax>();
    }

    [Fact] void should_leave_authored_syntax_unchanged() => SyntaxJson.StructurallyEqual(_raw, SyntaxJson.Deserialize(_before)).ShouldBeTrue();
    [Fact] void should_be_idempotent() => SyntaxJson.StructurallyEqual(_effective, ScreenActionSubjects.Resolve(_effective).Value!).ShouldBeTrue();

    [Fact]
    void should_retain_locations_of_every_authored_node()
    {
        var effective = Locations(_effective, "$").ToDictionary(entry => entry.Path, entry => entry.Node.Location);
        foreach (var (path, node) in Locations(_raw, "$")) effective[path].ShouldEqual(node.Location);
    }

    [Fact]
    void should_match_the_native_fixture_pack()
    {
        var regenerate = Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_ACTION_SUBJECT_FIXTURES") == "1";
        foreach (var (name, value) in _exports)
        {
            var path = Path.Combine(_directory, $"native.{name}.json");
            if (regenerate) File.WriteAllText(path, JsonSerializer.Serialize(value, _indented) + "\n");
            else Assert.True(JsonElement.DeepEquals(value, JsonElement.Parse(File.ReadAllText(path))), path);
        }
        Assert.False(regenerate, "Fixture pack regenerated; review and rerun without SCREENPLAY_REGENERATE_ACTION_SUBJECT_FIXTURES");
    }

    [Fact]
    void should_agree_with_typescript_on_authored_and_effective_members()
    {
        var context = JsonElement.Parse(File.ReadAllText(Path.Combine(_directory, "typescript.context.json")));
        Assert.True(JsonElement.DeepEquals(context.GetProperty("subjects"), _exports["context"].GetProperty("subjects")));
        foreach (var name in new[] { "raw.syntax", "effective.syntax" })
        {
            var expected = JsonElement.Parse(File.ReadAllText(Path.Combine(_directory, $"typescript.{name}.json")));
            Compare(expected, _exports[name], "$");
        }
    }

    sealed record SubjectScope(string? Screen, string? Slice, string[] Data);

    static IEnumerable<object> Subjects(JsonElement value, string path, SubjectScope inherited)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray().SelectMany((child, index) => Subjects(child, $"{path}[{index}]", inherited));
        }
        if (value.ValueKind != JsonValueKind.Object) return [];
        var kind = value.TryGetProperty("kind", out var tag) ? tag.GetString() : null;
        var scope = kind switch
        {
            "SliceSyntax" => inherited with { Slice = path },
            "ScreenSyntax" => inherited with { Screen = path, Data = [] },
            "ScreenContributionSyntax" => inherited with { Data = [] },
            _ => inherited
        };
        if (value.TryGetProperty("directives", out var directives))
        {
            var local = directives.EnumerateArray().Select((node, index) => (node, index)).Where(entry => entry.node.GetProperty("kind").GetString() == "ScreenDataSyntax").Select(entry => $"{path}.directives[{entry.index}]").ToArray();
            if (local.Length > 0) scope = scope with { Data = local };
        }
        var children = value.EnumerateObject().SelectMany(property => Subjects(property.Value, $"{path}.{property.Name}", scope));
        return (string.Equals(kind, "ScreenActionSyntax", StringComparison.Ordinal) || string.Equals(kind, "ScreenGuardedActionSyntax", StringComparison.Ordinal) || string.Equals(kind, "ToolbarItemSyntax", StringComparison.Ordinal))
            ? new object[] { new { path, screen = scope.Screen, slice = scope.Slice, data = scope.Data } }.Concat(children)
            : children;
    }

    static bool Relevant(Diagnostic diagnostic) => string.Equals(diagnostic.Code, DiagnosticCodes.InvalidInteractionArgument, StringComparison.Ordinal) || string.Equals(diagnostic.Code, DiagnosticCodes.UnknownActionSubjectField, StringComparison.Ordinal) || string.Equals(diagnostic.Code, DiagnosticCodes.UnresolvedActionSubject, StringComparison.Ordinal) || string.Equals(diagnostic.Code, DiagnosticCodes.InvalidActionSubjectClause, StringComparison.Ordinal) || string.Equals(diagnostic.Code, DiagnosticCodes.InferredActionSubject, StringComparison.Ordinal) || string.Equals(diagnostic.Code, DiagnosticCodes.ChangedActionSubject, StringComparison.Ordinal) || string.Equals(diagnostic.Code, DiagnosticCodes.AmbiguousActionInputBinding, StringComparison.Ordinal) || string.Equals(diagnostic.Code, DiagnosticCodes.UnsupportedActionSubjectContext, StringComparison.Ordinal);

    static IEnumerable<(string Path, SyntaxNode Node)> Locations(SyntaxNode node, string path)
    {
        yield return (path, node);
        foreach (var member in SyntaxKinds.For(node.GetType()).Members)
        {
            foreach (var entry in MemberLocations(member.Property.GetValue(node), $"{path}.{member.Name}")) yield return entry;
        }
    }

    static IEnumerable<(string Path, SyntaxNode Node)> MemberLocations(object? value, string path)
    {
        if (value is SyntaxNode child)
        {
            foreach (var entry in Locations(child, path)) yield return entry;
            yield break;
        }
        if (value is not IEnumerable items || value is string) yield break;

        var index = 0;
        foreach (var item in items)
        {
            if (item is SyntaxNode element)
            {
                foreach (var entry in Locations(element, $"{path}[{index}]")) yield return entry;
            }
            index++;
        }
    }

    static void Compare(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(actual.ValueKind == expected.ValueKind, path);
        if (expected.ValueKind == JsonValueKind.Object)
        {
            CompareProperties(expected, actual, path);
        }
        else if (expected.ValueKind == JsonValueKind.Array)
        {
            CompareElements(expected, actual, path);
        }
        else
        {
            Assert.True(JsonElement.DeepEquals(expected, actual), path);
        }
    }

    static void CompareProperties(JsonElement expected, JsonElement actual, string path)
    {
        foreach (var property in expected.EnumerateObject())
        {
            Assert.True(actual.TryGetProperty(property.Name, out var value), $"{path}.{property.Name}");
            Compare(property.Value, value, $"{path}.{property.Name}");
        }
    }

    static void CompareElements(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(actual.GetArrayLength() == expected.GetArrayLength(), path);
        for (var index = 0; index < expected.GetArrayLength(); index++) Compare(expected[index], actual[index], $"{path}[{index}]");
    }

    static string Fixtures([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return Path.Combine(directory!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "action-subjects");
    }
}
