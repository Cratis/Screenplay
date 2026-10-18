// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_stream_id_parts
{
    const string Source = """
        eventsource Account
          id "StoredAccount"
          identifier String
          stream Ledger
            id "StoredLedger"
            streamId
              account String
              period String // period stays in this comment
          stream Other
            streamId
              account String
              period String
        module M
          feature F
            slice StateChange S
              event Recorded
              command Record
                account String identifier
                period String
                stream Account.Ledger
                  streamId
                    period = period
                    account = account
                produces Recorded
                  for account
              command OtherRecord
                account String identifier
                stream Account.Other
                  streamId
                    period = "period"
                    account = account
                produces Recorded
                  for account
              example Prior : Recorded
                for "a"
                stream Account.Ledger
                  streamId
                    period = "p|%"
                    account = "a"
              specification Routed
                given Prior
                when Record
                  account = "a"
                  period = "p|%"
                then Recorded
                  stream Account.Ledger
                    streamId
                      account = "a"
                      period = "p|%"
              specification Appended
                when append Recorded
                  for "a"
                  stream Account.Ledger
                    streamId
                      period = "p|%"
                      account = "a"
                then Recorded
                  no stream
            slice Automation Recovery
              reaction Observer
                when Recorded
              specification Redelivery
                given Recorded
                  for "a"
                  stream Account.Ledger
                    streamId
                      account = "a"
                      period = "p|%"
                when redelivered Recorded to Observer
                  stream Account.Ledger
                    streamId
                      period = "p|%"
                      account = "a"
                then no events
        """;

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, false)]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, true)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, false)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, true)]
    void should_rename_only_bound_mappings_and_keep_order_values_and_identities(WorkspaceAuthoringFormatting formatting, bool separateFiles)
    {
        var split = Source.IndexOf("module M", StringComparison.Ordinal);
        var workspace = separateFiles ? Workspace(Source[..split], Source[split..]) : Workspace(Source);
        var result = Rename(workspace, "month", formatting);
        result.Conflicts.ShouldBeEmpty();
        result.Accepted.ShouldBeTrue();
        var before = WorkspaceSyntaxIndex.Create(workspace).Entries;
        var after = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries;
        var stream = after.Select(entry => entry.Node).OfType<EventStreamSyntax>().Single(stream => stream.Name == "Ledger");
        stream.StreamIdParts.Select(part => part.Name).ShouldEqual(["account", "month"]);
        stream.Id.ShouldEqual("StoredLedger");
        stream.StreamIdParts.Select(part => part.Type).Select(type => type.Name).ShouldEqual(["String", "String"]);
        after.Select(entry => entry.Node).OfType<EventSourceSyntax>().Single().Id.ShouldEqual("StoredAccount");
        after.Select(entry => entry.Node).OfType<EventStreamSyntax>().Single(stream => stream.Name == "Other").StreamIdParts.Select(part => part.Name).ShouldEqual(["account", "period"]);
        foreach (var original in before.Where(entry => entry.Node is CommandStreamSyntax or SpecificationStreamSyntax))
        {
            var candidate = after.Single(entry => entry.Handle.Path == original.Handle.Path && entry.Handle.Document == original.Handle.Document);
            var oldParts = Parts(original.Node).ToArray();
            var newParts = Parts(candidate.Node).ToArray();
            var renamed = original.Node is not CommandStreamSyntax { Stream: "Other" };
            newParts.Select(part => part.Property).ShouldEqual(oldParts.Select(part => renamed && part.Property == "period" ? "month" : part.Property));
            newParts.Select(part => Value(part.Source)).ShouldEqual(oldParts.Select(part => Value(part.Source)));
        }
        foreach (var original in before.Where(entry => entry.SemanticId is not null))
        {
            after.Single(entry => entry.Handle.Path == original.Handle.Path && entry.Handle.Document == original.Handle.Document).SemanticId.ShouldEqual(original.SemanticId);
        }
        result.Workspace!.IdentityCatalog.Revision.ShouldEqual(workspace.IdentityCatalog.Revision);
        StoredId(workspace).ShouldEqual("a|p%7C%25");
        StoredId(result.Workspace).ShouldEqual(StoredId(workspace));
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            string.Concat(result.Workspace.Documents.Select(document => document.Text)).ShouldEqual(Source.Replace("period String //", "month String //", StringComparison.Ordinal)
                .Replace("period = period", "month = period", StringComparison.Ordinal)
                .Replace("\n            period = \"p|%\"", "\n            month = \"p|%\"", StringComparison.Ordinal)
                .Replace("\n              period = \"p|%\"", "\n              month = \"p|%\"", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_refuse_a_rename_with_a_sibling_part_collision(WorkspaceAuthoringFormatting formatting) =>
        Refused(Rename(Workspace(Source), "account", formatting), "already declares stream id part 'account'");

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_refuse_a_rename_with_an_unresolved_route(WorkspaceAuthoringFormatting formatting)
    {
        var split = Source.IndexOf("module M", StringComparison.Ordinal);
        var workspace = Workspace(Source[..split], Source[split..].Replace("stream Account.Ledger\n", "stream Missing.Ledger\n", StringComparison.Ordinal));
        Refused(Rename(workspace, "month", formatting), "Every document must parse");
    }

    static string? StoredId(ScreenplayWorkspace workspace)
    {
        workspace.Compilation.Success.ShouldBeTrue();
        var model = workspace.Compilation.Value!.Model;
        var specification = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications).Single(specification => specification.Name == "Routed");
        var run = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, specification.Id);
        return ((SemanticAccepted)run.Execution).Facts.Single().Route!.StreamId;
    }

    static object? Value(ExpressionSyntax expression) => expression switch
    {
        PathExpressionSyntax path => path.Path,
        LiteralExpressionSyntax literal => literal.Value,
        _ => null
    };

    static IEnumerable<PropertyMappingSyntax> Parts(SyntaxNode node) => node switch
    {
        CommandStreamSyntax route => route.StreamIdParts,
        SpecificationStreamSyntax route => route.StreamIdParts,
        _ => []
    };

    static ScreenplayWorkspace Workspace(params string[] sources)
    {
        var documents = sources.Select((source, index) => WorkspaceDocument.Create($"model{index}", PortablePlayPath.Parse($"model{index}.play"), Encoding.UTF8.GetBytes(source))).ToArray();
        var workspace = ScreenplayWorkspace.Create("A", [.. documents], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var entries = WorkspaceSyntaxIndex.Create(workspace).Entries;
        var catalog = SemanticIdentityCatalog.PlanMigration(
            workspace.IdentityCatalog,
            workspace.IdentityCatalog.Revision,
            [.. documents.Select(document => document.StableKey)],
            [.. entries.Where(entry => entry.Address is not null).Select(entry => entry.Address!).Distinct()],
            [.. entries.Where(entry => entry.Node is EventSyntax).Select(entry => entry.Address!).Distinct()],
            [],
            [],
            []).Catalog;
        return ScreenplayWorkspace.Create("A", [.. documents], catalog);
    }

    static WorkspaceAuthoringResult Rename(ScreenplayWorkspace workspace, string name, WorkspaceAuthoringFormatting formatting) => workspace.ProposeRename(new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Target = WorkspaceSyntaxIndex.Create(workspace).Entries.First(entry => entry.Node is EventStreamIdPartSyntax part && part.Name == "period").Handle,
        ExpectedName = "period",
        NewName = name,
        Formatting = formatting
    });

    static void Refused(WorkspaceAuthoringResult result, string message)
    {
        result.Accepted.ShouldBeFalse();
        result.WritePlan.ShouldBeNull();
        result.Conflicts.Single().Message.ShouldContain(message);
    }
}
