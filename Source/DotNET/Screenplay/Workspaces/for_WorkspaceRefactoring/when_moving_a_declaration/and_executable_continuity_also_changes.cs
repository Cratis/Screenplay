// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_executable_continuity_also_changes : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var source = Model().Replace("  feature Destination", "  feature Destination\n    authorize CanLookup", StringComparison.Ordinal)
            + "\npolicy CanLookup\n  require role \"Reader\"";
        Workspace = ScreenplayWorkspace.Create("Projects", [Document("model", "application.play", source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        Assert.True(Workspace.Compilation.Success, CompilationDiagnostics(Workspace));
    }

    void Because() => _result = Workspace.ProposeMove(Request(SemanticAddress.ForQuery(Slice("Source", "Listing"), "AllProjects"), Slice("Destination", "Lookup")));

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_preserve_the_inherited_authorize_continuity_refusal() => Refusals(_result).ShouldContain("Inherited authorize or screen on/uses interaction bindings differ at 'Projects.Source.Listing.AllProjects' -> 'QuerySyntax:Projects.Destination.Lookup.AllProjects': previous [], current [");
    [Fact] void should_name_the_new_inherited_policy() => Refusals(_result).ShouldContain("CanLookup");
    [Fact] void should_not_replace_it_with_the_new_denial_obligation_refusal() => Refusals(_result).ShouldNotContain("specification obligations unmet");
    [Fact] void should_return_no_candidate() => _result.Workspace.ShouldBeNull();
}
