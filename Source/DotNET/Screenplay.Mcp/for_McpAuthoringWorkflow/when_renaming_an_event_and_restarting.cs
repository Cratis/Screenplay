// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_renaming_an_event_and_restarting : given.an_authoring_connection
{
    byte[] _original = [];
    byte[] _expected = [];
    byte[] _reviewed = [];
    JsonElement _proposal;
    JsonElement _applied;
    JsonElement _reopened;
    string[] _semanticIds = [];
    string[] _reopenedSemanticIds = [];
    string[] _eventIds = [];
    string[] _reopenedEventIds = [];
    bool _unchanged;

    void Establish()
    {
        const string comment = "\uFEFF// ProjectRegistered remains in this comment\r\n";
        _original = Encoding.UTF8.GetBytes(comment + Source.Replace("\n", "\r\n", StringComparison.Ordinal));
        _expected = Encoding.UTF8.GetBytes(comment + Source.Replace("ProjectRegistered", "ProjectEnrolled", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal));
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), _original);
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        _semanticIds = Ids("semantics", "semanticId", revision);
        _eventIds = Ids("eventContracts", "eventContractId", revision);
        var target = Node("EventSyntax", revision).GetProperty("handle");
        _proposal = Result("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target,
            expectedName = "ProjectRegistered",
            newName = "ProjectEnrolled"
        });
        _reviewed = Result("read-proposal", new
        {
            proposalId = _proposal.GetProperty("proposalId").GetString(),
            view = "after",
            documentId = target.GetProperty("documentId").GetString()
        }).GetProperty("result").GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64();
        _unchanged = File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(_original);
        _applied = Apply(opened, _proposal);
        Connection = new(new McpTools(new McpRoot(RootPath)));
        Initialize();
        _reopened = Open();
        var reopenedRevision = _reopened.GetProperty("revision").GetString()!;
        _reopenedSemanticIds = Ids("semantics", "semanticId", reopenedRevision);
        _reopenedEventIds = Ids("eventContracts", "eventContractId", reopenedRevision);
    }

    [Fact] void should_review_exact_bytes_preserving_comment_bom_and_crlf() => _reviewed.ShouldEqual(_expected);
    [Fact] void should_leave_disk_untouched_until_apply() => _unchanged.ShouldBeTrue();
    [Fact] void should_apply_the_renamed_source() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_install_the_exact_reviewed_bytes() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_expected);
    [Fact] void should_have_assigned_semantic_identities() => _semanticIds.Length.ShouldBeGreaterThan(5);
    [Fact] void should_have_assigned_an_event_identity() => _eventIds.Length.ShouldEqual(1);
    [Fact] void should_migrate_semantic_ids_automatically_and_preserve_them_after_restart() => _reopenedSemanticIds.ShouldContainOnly(_semanticIds);
    [Fact] void should_migrate_event_ids_automatically_and_preserve_them_after_restart() => _reopenedEventIds.ShouldContainOnly(_eventIds);
    [Fact] void should_reopen_the_applied_revision_without_export() => _reopened.GetProperty("revision").GetString().ShouldEqual(_proposal.GetProperty("after").GetProperty("revision").GetString());
    [Fact] void should_keep_safe_reference_validation_visible() => _proposal.GetProperty("referencePolicy").GetString().ShouldEqual("Safe");

    string[] Ids(string view, string field, string revision) => [.. Page(view, revision).EnumerateArray().Select(item => item.GetProperty(field).GetString()!)];
}
