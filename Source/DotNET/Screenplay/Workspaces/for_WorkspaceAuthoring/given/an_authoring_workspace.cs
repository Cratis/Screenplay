// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.given;

public class an_authoring_workspace : for_ScreenplayWorkspace.given.a_valid_workspace
{
    protected WorkspaceSyntaxIndex Index = null!;
    protected WorkspaceSyntaxEntry RegistrationRoot = null!;
    protected WorkspaceSyntaxEntry ConceptsRoot = null!;

    void Establish()
    {
        Index = WorkspaceSyntaxIndex.Create(Workspace);
        RegistrationRoot = Index.Entries.Single(entry => entry.Handle.Document == Registration.Id && entry.Parent is null);
        ConceptsRoot = Index.Entries.Single(entry => entry.Handle.Document == Concepts.Id && entry.Parent is null);
    }

    protected WorkspaceAuthoringRequest Authoring(params WorkspaceAstOperation[] operations) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Operations = [.. operations]
    };

    protected static ApplicationSyntax Syntax(string source) => new ScreenplayCompiler().Parse(source).Value!;
}
