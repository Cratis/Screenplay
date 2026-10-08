// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_reaction_invocation_changes : given.a_reactive_comparison
{
    void Because() => CompareSnapshots(ReactiveSource, ReactiveSource.Replace("invokes First", "invokes Second", StringComparison.Ordinal));

    [Fact] void should_compare_reactions_using_their_catalog_ids() => Items("members").Any(item => item.GetProperty("kind").GetString() == "Reaction" && item.GetProperty("semanticId").GetString() is not null).ShouldBeTrue();
    [Fact] void should_have_assigned_reaction_ids_to_compare() => Workspace.IdentityCatalog.Semantics.Count(assignment => assignment.Address.Kind == SemanticKind.Reaction).ShouldEqual(2);
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
