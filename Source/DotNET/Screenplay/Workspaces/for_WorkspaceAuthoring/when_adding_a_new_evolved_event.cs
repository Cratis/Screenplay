// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_adding_a_new_evolved_event : Specification
{
    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        const string initial = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Unrelated\n        value String\n";
        const string evolved = initial + "      event Registered\n        old String\n      event Registered generation 2\n        current String\n";
        var document = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(initial));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _result = workspace.ProposeAuthoring(new WorkspaceAuthoringRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Executable,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, new ScreenplayCompiler().Parse(evolved).Value!)]
        });
    }

    [Fact] void should_admit_the_executable_edit() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_record_the_new_event_revision() => _result.Workspace!.IdentityCatalog.EventContracts.Single(assignment => assignment.Address.Name == "Registered").Revision.Value.ShouldEqual(2u);
    [Fact] void should_preserve_the_ordinary_event_revision() => _result.Workspace!.IdentityCatalog.EventContracts.Single(assignment => assignment.Address.Name == "Unrelated").Revision.Value.ShouldEqual(1u);
}
