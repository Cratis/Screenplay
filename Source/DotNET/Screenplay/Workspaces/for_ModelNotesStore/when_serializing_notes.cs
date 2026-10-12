// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ModelNotesStore;

public class when_serializing_notes : Specification
{
    ModelNotesDocument _document = null!;
    byte[] _bytes = null!;
    ApplicationIdentity _application;

    void Establish()
    {
        _application = ApplicationIdentity.Create("Library");
        var note = new ModelNote(ModelNotesStore.NewId(), "question", "May a member cancel?", new(Application: true), ModelNotesAddress.From(SemanticAddress.ForApplication(_application)), "sindre", DateTimeOffset.UtcNow, [new("D123", "https://example.org/D123")], null);
        _document = new(_application.ToString(), [note with { Id = "note_zzzzzzzzzzzzzzzzzzzzzz" }, note with { Id = "note_aaaaaaaaaaaaaaaaaaaaaa" }]);
    }

    void Because() => _bytes = ModelNotesStore.Serialize(_document);

    [Fact] void should_roundtrip() => ModelNotesStore.ReadBytes(_bytes, _application).Status.ShouldEqual("ok");
    [Fact] void should_be_byte_stable() => ModelNotesStore.Serialize(ModelNotesStore.ReadBytes(_bytes, _application).Document!).SequenceEqual(_bytes).ShouldBeTrue();
    [Fact] void should_have_a_trailing_lf() => _bytes[^1].ShouldEqual((byte)'\n');
    [Fact] void should_have_no_cr() => _bytes.Contains((byte)'\r').ShouldBeFalse();
    [Fact] void should_sort_by_identity() => (Encoding.UTF8.GetString(_bytes).IndexOf("note_aaa", StringComparison.Ordinal) < Encoding.UTF8.GetString(_bytes).IndexOf("note_zzz", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_write_one_note_per_line() => Encoding.UTF8.GetString(_bytes).Split('\n').Count(line => line.StartsWith("{\"id\":", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_use_a_separate_revision() => ModelNotesStore.ReadBytes(_bytes, _application).Revision!.StartsWith("nrev1:", StringComparison.Ordinal).ShouldBeTrue();

    [Fact]
    void should_include_the_required_document_keys()
    {
        using var actual = JsonDocument.Parse(_bytes);
        RequireKeys(Schema(), actual.RootElement);
    }

    [Fact]
    void should_match_the_schema_constants()
    {
        using var actual = JsonDocument.Parse(_bytes);
        foreach (var property in Schema().GetProperty("properties").EnumerateObject().Where(property => property.Value.TryGetProperty("const", out _))) actual.RootElement.GetProperty(property.Name).GetRawText().ShouldEqual(property.Value.GetProperty("const").GetRawText());
    }

    [Fact]
    void should_include_the_required_note_keys()
    {
        using var actual = JsonDocument.Parse(_bytes);
        var schema = Schema().GetProperty("$defs").GetProperty("note");
        foreach (var note in actual.RootElement.GetProperty("notes").EnumerateArray()) RequireKeys(schema, note);
    }

    static void RequireKeys(JsonElement schema, JsonElement actual)
    {
        foreach (var required in schema.GetProperty("required").EnumerateArray()) actual.TryGetProperty(required.GetString()!, out _).ShouldBeTrue();
    }

    static JsonElement Schema()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!IsRepository(repository!)) repository = repository!.Parent;
        using var schema = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository!.FullName, "Documentation/screenplay/notes-v1.schema.json")));
        return schema.RootElement.Clone();
    }

    static bool IsRepository(DirectoryInfo directory) => File.Exists(Path.Combine(directory.FullName, ".git")) || Directory.Exists(Path.Combine(directory.FullName, ".git"));
}
