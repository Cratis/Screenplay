// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_identifying_unbound_containers : given.an_unbound_container_workspace
{
    ScreenplayWorkspace _repeated = null!;
    WorkspaceAuthoringResult _fixed = null!;
    ScreenplayWorkspace _fresh = null!;

    void Because()
    {
        _repeated = Create(Source);
        var source = Source.Replace("Missing", "String", StringComparison.Ordinal);
        _fixed = Replace(Workspace, source);
        _fresh = Create(source);
    }

    [Fact] void should_remain_unbound() => Workspace.Compilation.Success.ShouldBeFalse();
    [Fact] void should_assign_the_module_nested_features_and_slice() => Workspace.IdentityCatalog.Semantics.Length.ShouldEqual(4);
    [Fact] void should_use_the_binding_bootstrap_assignments() => Workspace.IdentityCatalog.Semantics.All(assignment => assignment.Id == SemanticId.Create(assignment.Address) && assignment.Origin == SemanticIdentityOrigin.LegacyBootstrap).ShouldBeTrue();
    [Fact] void should_not_assign_in_slice_declarations() => Workspace.IdentityCatalog.Semantics.All(assignment => assignment.Address.Kind is SemanticKind.Module or SemanticKind.Feature or SemanticKind.Slice).ShouldBeTrue();
    [Fact] void should_not_assign_events() => Workspace.IdentityCatalog.EventContracts.ShouldBeEmpty();
    [Fact] void should_repeat_the_catalog_revision() => _repeated.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
    [Fact] void should_repeat_the_workspace_revision() => _repeated.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_accept_the_binding_fix() => Assert.True(_fixed.Accepted, string.Join("; ", _fixed.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_bind_the_fixed_source() => _fixed.Workspace!.Compilation.Success.ShouldBeTrue();
    [Fact] void should_preserve_every_earlier_id_and_origin() => Workspace.IdentityCatalog.Semantics.All(_fixed.Workspace!.IdentityCatalog.Semantics.Contains).ShouldBeTrue();
    [Fact] void should_match_binding_without_prebinding_materialization() => SemanticIdentityCatalogSerializer.Serialize(_fixed.Workspace!.IdentityCatalog).ShouldEqual(SemanticIdentityCatalogSerializer.Serialize(_fresh.IdentityCatalog));
}
