// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_editing_a_workspace_with_event_generations : Specification
{
    ScreenplayWorkspace _workspace = null!;
    WorkspaceAuthoringResult _authorable = null!;
    WorkspaceAuthoringResult _executable = null!;

    void Establish()
    {
        var events = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(
            "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        old String\n      event Registered generation 2\n        current String\n"));
        var concept = WorkspaceDocument.Create("concept", PortablePlayPath.Parse("concept.play"), Encoding.UTF8.GetBytes("concept Label : String\n"));
        _workspace = ScreenplayWorkspace.Create("Projects", [events, concept], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(concept.Id, new ScreenplayCompiler().Parse("concept Label : String\nconcept Title : String\n").Value!)]
        };
        _authorable = _workspace.ProposeAuthoring(request);
        _executable = _workspace.ProposeAuthoring(request with { Validation = WorkspaceAuthoringValidation.Executable });
    }

    [Fact] void should_accept_the_unrelated_edit() => Assert.True(_authorable.Accepted, string.Join("; ", _authorable.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_compile_the_fresh_event_generations() => _workspace.Compilation.Success.ShouldBeTrue();
    [Fact] void should_record_the_declared_revision() => _workspace.IdentityCatalog.EventContracts.Single().Revision.Value.ShouldEqual(2u);
    [Fact] void should_report_executable_readiness() => _authorable.ExecutableReady.ShouldBeTrue();
    [Fact] void should_accept_executable_validation() => _executable.Accepted.ShouldBeTrue();
}
