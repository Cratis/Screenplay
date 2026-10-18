// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_proposing_unchanged_unbound_container_sources : given.an_unbound_container_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Replace(Workspace, Source);

    [Fact] void should_accept_the_proposal() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_preserve_each_assignment_in_the_same_order() => _result.Workspace!.IdentityCatalog.Semantics.SequenceEqual(Workspace.IdentityCatalog.Semantics).ShouldBeTrue();
    [Fact] void should_materialize_in_the_binding_identity_order() => Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id.ToString()).ShouldEqual(Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id.ToString()).Order(StringComparer.Ordinal));
    [Fact] void should_not_add_in_slice_assignments() => _result.Workspace!.IdentityCatalog.Semantics.All(assignment => assignment.Address.Kind is SemanticKind.Module or SemanticKind.Feature or SemanticKind.Slice).ShouldBeTrue();
}
