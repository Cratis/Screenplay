// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_replacing_a_whole_property_mapping;

public class and_the_mapping_count_changes : given.a_document_with_literal_values
{
    void Because()
    {
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is ProducesSyntax);
        var produces = (ProducesSyntax)entry.Node;
        Result = Propose(new ReplaceWorkspaceNode(entry.Handle, entry.Node, produces with { Mappings = [.. produces.Mappings.Take(1)] }));
    }

    [Fact] void should_refuse_a_structural_change() => Result.Accepted.ShouldBeFalse();
    [Fact] void should_ask_for_explicit_canonical_printing() => Result.Conflicts.Single().Message.Contains("CanonicalizeTouchedDocuments", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_offer_no_write_plan() => Result.WritePlan.ShouldBeNull();
}
