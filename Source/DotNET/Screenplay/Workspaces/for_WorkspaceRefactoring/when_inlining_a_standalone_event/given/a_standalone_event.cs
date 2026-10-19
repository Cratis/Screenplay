// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_inlining_a_standalone_event.given;

public class a_standalone_event : Specification
{
    protected const string Eligible = "module Projects\n  feature Naming\n    slice StateChange Rename\n      event Renamed\n        id \"OldName\" // identity intent\n        description \"A new name\" // description intent\n        documentation\n          ```markdown\n          Name history.\n          ```\n        tag audit\n        name String\n      command Rename\n        projectId Uuid identifier\n        name String\n        produces Renamed // production intent\n          for projectId\n          // mapping intent\n          name = name // same comment\n";
    protected ScreenplayWorkspace _workspace;
    protected WorkspaceAuthoringResult _result;

    protected virtual string Source => Eligible;

    protected static string[] Comments(ScreenplayWorkspace workspace) => [.. workspace.Documents.SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens).Where(value => value.Kind == WorkspaceSourceTokenKind.Comment).Select(value => value.Text).Order(StringComparer.Ordinal)];

    void Establish() => _workspace = Create(Source);

    protected static ScreenplayWorkspace Create(string source) =>
        ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    protected WorkspaceAuthoringResult Inline(ScreenplayWorkspace workspace, WorkspaceAuthoringFormatting formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
    {
        var subject = WorkspaceSyntaxIndex.Create(workspace).Entries.First(entry => entry.Node is EventSyntax { Name: "Renamed" });
        return WorkspaceEventRefactorings.ProposeInlineStandaloneEvent(workspace, subject.Handle, new WorkspaceAuthoringRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = formatting,
            Validation = WorkspaceAuthoringValidation.Authoring
        });
    }

    protected WorkspaceAuthoringResult Inline() => Inline(_workspace);
}
