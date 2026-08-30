// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_a_non_slice_semantic_id : given.a_workspace_with_a_described_slice
{
    WorkspaceTransactionResult _moduleResult = null!;
    WorkspaceTransactionResult _featureResult = null!;
    WorkspaceTransactionResult _commandResult = null!;

    void Because()
    {
        var application = Workspace.Compilation.Value!.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();

        _moduleResult = Update(module.Id);
        _featureResult = Update(feature.Id);
        _commandResult = Update(command.Id);
    }

    WorkspaceTransactionResult Update(SemanticId id) => Workspace.Propose(Request(new UpdateSliceDescription
    {
        SemanticId = id,
        ExpectedCurrentDescription = OriginalDescription,
        NewDescription = "Registers a brand new project"
    }));

    [Fact] void should_reject_the_module_id_as_an_unsupported_semantic_field() => _moduleResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
    [Fact] void should_reject_the_feature_id_as_an_unsupported_semantic_field() => _featureResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
    [Fact] void should_reject_the_command_id_as_an_unsupported_semantic_field() => _commandResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
}
