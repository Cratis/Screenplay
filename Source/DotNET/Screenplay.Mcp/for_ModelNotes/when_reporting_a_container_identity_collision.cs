// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_reporting_a_container_identity_collision : given.a_split_notes_workspace
{
    readonly List<Exception> _refusals = [];

    void Establish()
    {
        OpenSplit(unbound: true);
        var slice = Split.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice);
        var module = Split.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Module);
        var catalog = SemanticIdentityCatalog.Create(Split.IdentityCatalog.Application, [], [new(module.Address, slice.Id, SemanticIdentityOrigin.Persisted)], []);
        Split = ScreenplayWorkspace.Create("Acme", Root.Read(), catalog);
        Index = WorkspaceSyntaxIndex.Create(Split);
        Opened = Result(Workspaces.Open(Arguments(new { workspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(Split)) })));
    }

    void Because()
    {
        foreach (var target in Targets(SemanticKind.Slice)) _refusals.Add(Catch.Exception(() => ProposeNote(target)));
    }

    [Fact] void should_report_the_collision() => Opened.GetProperty("containerIdentities").GetProperty("containers")[0].GetProperty("reason").GetString().ShouldEqual("idCollision");
    [Fact] void should_report_no_slice_assignment() => Opened.GetProperty("containerIdentities").GetProperty("containers")[0].GetProperty("assigned").GetBoolean().ShouldBeFalse();
    [Fact] void should_refuse_dotted_typed_and_handle_targets() => _refusals.TrueForAll(failure => failure is McpFailure && failure.Message.Contains("idCollision", StringComparison.Ordinal)).ShouldBeTrue();
}
