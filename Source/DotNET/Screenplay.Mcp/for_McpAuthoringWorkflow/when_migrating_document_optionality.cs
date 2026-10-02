// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_migrating_document_optionality : given.an_authoring_connection
{
    const string Types = "// café?\r\ntype Details\r\n  note    String? // keep?\r\n  lines   String[]?\r\n";
    JsonElement _opened;
    JsonElement _repair;
    JsonElement _proposal;
    byte[] _before;
    byte[] _preview;

    void Establish()
    {
        _before = [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Types + Source)];
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), _before);
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        var repairs = Page("repairs", _opened.GetProperty("revision").GetString()!).EnumerateArray()
            .Where(repair => repair.GetProperty("diagnosticCode").GetString() == "PLAY0479").ToArray();
        repairs.Length.ShouldEqual(3);
        _repair = repairs.Single(repair => repair.GetProperty("scope").GetString() == "document");
        _proposal = Result("propose-repair", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0479",
            subject = _repair.GetProperty("subject"),
            formatting = "PreserveTrivia"
        });
        _preview = [.. Candidate(_proposal).Documents.Single().Bytes];
    }

    [Fact] void should_describe_typed_spelling_operations() => _repair.GetProperty("operations").EnumerateArray().All(operation => operation.GetProperty("operation").GetString() == "migrate-optional-type").ShouldBeTrue();
    [Fact] void should_require_trivia_preservation_by_default() => _repair.GetProperty("requiredFormatting").GetString().ShouldEqual("PreserveTrivia");
    [Fact] void should_not_write_during_discovery_or_preview() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_before);
    [Fact] void should_keep_every_other_byte() => _preview.ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Types.Replace("String?", "String optional", StringComparison.Ordinal).Replace("String[]?", "String[] optional", StringComparison.Ordinal) + Source)]);

    [Fact]
    void should_apply_only_after_explicit_acceptance()
    {
        Apply(_opened, _proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_preview);
    }
}
