// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_repairing_partial_read_model_keys : given.an_authoring_connection
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      readmodel Row\n        id String key\n        period Int key\n      command C\n        id String\n        period Int\n        reads Row by id\n";

    [Fact]
    void should_preview_a_complete_named_key_without_writing()
    {
        File.WriteAllText(Path.Combine(RootPath, "keys.play"), Source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var repair = Page("repairs", revision).EnumerateArray().Single(item => item.GetProperty("diagnosticCode").GetString() == "PLAY0634");
        var proposal = Result("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0634",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        proposal.GetProperty("proposalId").ValueKind.ShouldEqual(JsonValueKind.String);
        File.ReadAllText(Path.Combine(RootPath, "keys.play")).ShouldEqual(Source);
        var candidate = Candidate(proposal);
        candidate.Documents.Single(document => document.Path.Value == "keys.play").Text.ShouldContain("period = period");
    }

    [Fact]
    void should_resolve_each_composite_absence_part_type()
    {
        var source = Source.Replace("        reads Row by id\n", string.Empty, StringComparison.Ordinal) +
            "      specification Missing\n        then no readmodel Row for {\"id\":\"r\",\"period\":1}\n";
        File.WriteAllText(Path.Combine(RootPath, "keys.play"), source);
        Initialize();
        Open();
        var parts = Result("find-fixtures", new { role = "thenAbsentReadModelKeyPart" }).GetProperty("page").GetProperty("items").EnumerateArray().ToArray();
        parts.Length.ShouldEqual(2);
        parts.Select(part => part.GetProperty("declaredType").GetProperty("name").GetString()).ShouldEqual("String", "Int");
    }

    [Fact]
    void should_resolve_an_explicit_single_absence_key_without_a_query()
    {
        var source = Source.Replace("period Int key", "period Int", StringComparison.Ordinal).Replace("        reads Row by id\n", string.Empty, StringComparison.Ordinal) +
            "      specification Missing\n        then no readmodel Row for \"r\"\n";
        File.WriteAllText(Path.Combine(RootPath, "keys.play"), source);
        Initialize();
        Open();
        var destination = Result("find-fixtures", new { role = "thenAbsentReadModelDestination" }).GetProperty("page").GetProperty("items").EnumerateArray().Single();
        destination.GetProperty("declaredType").GetProperty("name").GetString().ShouldEqual("String");
    }

    [Fact]
    void should_advertise_only_the_missing_key_repair()
    {
        Initialize();
        var codes = Result("repair-capabilities").GetProperty("actions").EnumerateArray().Select(action => action.GetProperty("diagnosticCode").GetString()).ToArray();
        codes.ShouldContain("PLAY0634");
        codes.ShouldNotContain("PLAY0633");
        codes.ShouldNotContain("PLAY0635");
    }
}
