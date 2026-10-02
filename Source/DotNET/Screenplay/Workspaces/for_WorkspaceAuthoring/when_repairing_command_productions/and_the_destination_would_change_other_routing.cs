// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_destination_would_change_other_routing : given.a_command_production
{
    [Fact]
    void should_refuse_a_version_upgrade_that_promotes_an_untouched_command()
    {
        Create(DestinationSource.Replace(VersionAnchor, string.Empty, StringComparison.Ordinal).Replace("          registeredAt = $context.occurred\n", string.Empty, StringComparison.Ordinal).Replace("        registeredAt DateTime\n", string.Empty, StringComparison.Ordinal) + "\n      command Legacy\n        legacyId Uuid identifier\n        produces LegacyProduced\n          for legacyId\n          legacyId = legacyId\n      event LegacyProduced\n        legacyId Uuid\n");
        Workspace.Compilation.Success.ShouldBeTrue();
        var before = Workspace.Compilation.Value!.Model;
        before.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        before.Application.Modules[0].Features[0].Slices[0].Commands[1].Destination.ShouldBeNull();
        var candidate = PreviewDestination();
        candidate.Compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
        candidate.Compilation.Value.Model.Application.Modules[0].Features[0].Slices[0].Commands[1].Destination.ShouldNotBeNull();
        HasRepair(DiagnosticCodes.OmittedProductionDestination).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_rerouting_a_plain_sibling_even_without_a_version_change()
    {
        Create(DestinationSource.Replace("      event ProjectRegistered", "        produces ProjectRegistered\n          name = name\n          registeredAt = $context.occurred\n      event ProjectRegistered", StringComparison.Ordinal));
        Workspace.Compilation.Success.ShouldBeTrue();
        var before = Workspace.Compilation.Value!.Model;
        var candidate = PreviewDestination();
        candidate.Compilation.Value!.Model.SemanticVersion.ShouldEqual(before.SemanticVersion);
        before.Application.Modules[0].Features[0].Slices[0].Commands[0].Destination.ShouldBeNull();
        candidate.Compilation.Value.Model.Application.Modules[0].Features[0].Slices[0].Commands[0].Destination.ShouldNotBeNull();
        HasRepair(DiagnosticCodes.OmittedProductionDestination).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_when_the_original_has_no_executable_model()
    {
        Create(Source.Replace("          for projectId\n", string.Empty, StringComparison.Ordinal));
        Workspace.Compilation.Value.ShouldBeNull();
        Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldBeTrue();
        HasRepair(DiagnosticCodes.OmittedProductionDestination).ShouldBeFalse();
    }

    ScreenplayWorkspace PreviewDestination()
    {
        var subject = WorkspaceSyntaxIndex.Create(Workspace).Entries.First(entry => entry.Node is ProducesSyntax production && production.Event == "ProjectRegistered");
        var produces = (ProducesSyntax)subject.Node;
        var result = Workspace.ProposeAuthoring(Request() with
        {
            Operations = [new ReplaceWorkspaceNode(subject.Handle, produces, produces with { For = new PathExpressionSyntax("projectId", produces.Location) })]
        });
        result.Accepted.ShouldBeTrue();
        return result.Workspace!;
    }
}
