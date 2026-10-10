// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_removing_duplicate_compliance_markers : given.an_authoring_connection
{
    const string Concepts = "// keep\r\nconcept PersonalNote : String pii personal\r\n  pii reason \"Keep pii personal verbatim\"\r\n";
    JsonElement _opened;
    JsonElement _repair;
    JsonElement _proposal;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Concepts + Source);
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        _repair = Page("repairs", _opened.GetProperty("revision").GetString()!).EnumerateArray()
            .Single(repair => repair.GetProperty("diagnosticCode").GetString() == "PLAY0653");
        _proposal = Result("propose-repair", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0653",
            subject = _repair.GetProperty("subject"),
            formatting = "PreserveTrivia"
        });
    }

    [Fact] void should_discover_a_trivia_preserving_repair() => _repair.GetProperty("requiredFormatting").GetString().ShouldEqual("PreserveTrivia");
    [Fact] void should_describe_the_typed_operation() => _repair.GetProperty("operations")[0].GetProperty("operation").GetString().ShouldEqual("remove-duplicate-compliance-markers");
    [Fact] void should_remove_only_the_duplicate_header_token() => Candidate(_proposal).Documents.Single().Text.ShouldEqual(Concepts.Replace("String pii personal", "String pii", StringComparison.Ordinal) + Source);
    [Fact] void should_not_write_during_discovery_or_preview() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Concepts + Source);
}
