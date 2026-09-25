// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_editing_an_evolved_event_workspace_with_unrelated_binding_error : Specification
{
    WorkspaceAuthoringResult _result = null!;
    SemanticIdentityCatalog _before = null!;

    void Because()
    {
        var events = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(
            "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        old String\n      event Registered generation 2\n        current String\n"));
        var concept = WorkspaceDocument.Create("concept", PortablePlayPath.Parse("concept.play"), Encoding.UTF8.GetBytes("concept Label : String\n"));
        var workspace = ScreenplayWorkspace.Create("Projects", [events, concept], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _before = workspace.IdentityCatalog;
        var bad = new ScreenplayCompiler().Parse("import External.Unused\nconcept Label : String\n").Value!;
        var broken = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(concept.Id, bad)]
        });
        Assert.True(broken.Accepted, string.Join("; ", broken.Conflicts.Select(conflict => conflict.Message)));
        broken.ExecutableReady.ShouldBeFalse();
        _result = broken.Workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = broken.Workspace.Revision,
            ExpectedCatalogRevision = broken.Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(concept.Id, new ScreenplayCompiler().Parse("import External.Unused\nconcept Label : String\nconcept Title : String\n").Value!)]
        });
    }

    [Fact] void should_accept_the_unrelated_authorable_edit() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_preserve_generation_qualified_property_ids() => _before.Semantics.Where(assignment => assignment.Address.Kind == SemanticKind.Property)
        .All(assignment => _result.Workspace.IdentityCatalog.Semantics.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id)).ShouldBeTrue();
}
