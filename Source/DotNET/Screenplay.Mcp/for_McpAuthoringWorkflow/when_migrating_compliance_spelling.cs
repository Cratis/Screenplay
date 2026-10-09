// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_migrating_compliance_spelling : given.an_authoring_connection
{
    const string Concepts = "// café @pii\r\nconcept PersonalNote : String @pii @sensitive\r\n  sensitive reason \"Keep @pii and legal text\"\r\n";
    JsonElement _opened;
    JsonElement _repair;
    JsonElement _proposal;
    JsonElement[] _repairs;
    byte[] _before;

    void Establish()
    {
        _before = [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Concepts + Source)];
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), _before);
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        _repairs = [.. Page("repairs", _opened.GetProperty("revision").GetString()!).EnumerateArray()
            .Where(repair => repair.GetProperty("diagnosticCode").GetString() == "PLAY0560")];
        _repair = _repairs.Single(repair => repair.GetProperty("scope").GetString() == "document");
        _proposal = Result("propose-repair", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0560",
            subject = _repair.GetProperty("subject"),
            formatting = "PreserveTrivia"
        });
    }

    [Fact] void should_offer_two_lines_and_one_document() => _repairs.Length.ShouldEqual(3);
    [Fact] void should_describe_typed_spelling_operations() => _repair.GetProperty("operations").EnumerateArray().All(operation => operation.GetProperty("operation").GetString() == "migrate-compliance-marker").ShouldBeTrue();
    [Fact] void should_not_write_during_discovery_or_preview() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_before);
    [Fact] void should_preserve_notes_bom_and_line_endings() => Candidate(_proposal).Documents.Single().Bytes.ToArray().ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(Concepts.Replace("String @pii @sensitive", "String pii secret", StringComparison.Ordinal).Replace("  sensitive reason", "  secret reason", StringComparison.Ordinal) + Source)]);

    [Fact]
    void should_select_one_directive_line_without_migrating_the_header()
    {
        var repair = _repairs.Single(repair => repair.GetProperty("scope").GetString() == "occurrence" && repair.GetProperty("location").GetProperty("line").GetInt32() == 3);
        var proposal = Result("propose-repair", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0560",
            subject = repair.GetProperty("subject"),
            line = 3,
            formatting = "PreserveTrivia"
        });
        Candidate(proposal).Documents.Single().Text.ShouldEqual(Concepts.Replace("  sensitive reason", "  secret reason", StringComparison.Ordinal) + Source);
    }
}
