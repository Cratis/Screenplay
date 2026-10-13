// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_preserving_exact_source;

public class and_appending_a_feature : given.an_exact_source_connection
{
    static readonly string Supplied = Original + "\n\n  feature Archive\n    slice StateView Timeline\n      readmodel ArchivedProjects\n        name ProjectName\n      query GetArchivedProjects => ArchivedProjects[]";
    JsonElement _proposal;
    string[] _before = [];
    string[] _after = [];

    void Establish()
    {
        Apply(Opened, Result("propose-source", Arguments(ReplaceSource(Original))));
        Opened = Open();
        _before = [.. Page("semantics", Opened.GetProperty("revision").GetString()).EnumerateArray().Select(item => item.GetRawText())];
    }

    void Because()
    {
        _proposal = Result("propose-source", Arguments(ReplaceSource(Supplied)));
        Apply(Opened, _proposal);
        _after = [.. Page("semantics", Open().GetProperty("revision").GetString()).EnumerateArray().Select(item => item.GetRawText())];
    }

    [Fact] void should_keep_alignment_comments_and_blank_lines() => File.ReadAllBytes(ApplicationPath).SequenceEqual(Encoding.UTF8.GetBytes(Supplied)).ShouldBeTrue();
    [Fact] void should_keep_existing_assigned_semantic_identities() => _before.All(_after.Contains).ShouldBeTrue();
    [Fact] void should_have_existing_identities_to_compare() => _before.Length.ShouldBeGreaterThan(0);
    [Fact] void should_append_the_feature() => Result("read-ast", new { expectedRevision = Open().GetProperty("revision").GetString(), kind = "FeatureSyntax", name = "Archive" }).GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_drop_no_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_emit_no_normalization_diagnostic() => _proposal.GetProperty("authoringDiagnosticSeverities").GetProperty("codes").EnumerateArray().Select(code => code.GetString()).ShouldNotContain("PLAY0288");
}
