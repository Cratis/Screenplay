// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_refactoring_workspace : for_ScreenplayWorkspace.given.a_valid_workspace
{
    protected WorkspaceRenameRequest Rename<T>(string expectedName, string newName)
        where T : SyntaxNode => new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = WorkspaceSyntaxIndex.Create(Workspace).Entries.First(entry => entry.Node is T && WorkspaceReferenceBindings.Name(entry.Node) == expectedName).Handle,
            ExpectedName = expectedName,
            NewName = newName
        };
}
