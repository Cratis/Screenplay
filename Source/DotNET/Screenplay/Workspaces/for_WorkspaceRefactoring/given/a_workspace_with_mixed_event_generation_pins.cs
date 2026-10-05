// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_workspace_with_mixed_event_generation_pins : Specification
{
    protected const string Source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      event Renamed generation 1 // first generation\n        id \"Renamed\"\n        name   String // property intent\n      event Renamed generation 2 // second generation\n        name String\n";
    protected ScreenplayWorkspace Workspace = null!;

    protected static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    protected static IEnumerable<EventSyntax> Events(ScreenplayWorkspace workspace) => WorkspaceSyntaxIndex.Create(workspace).Entries.Select(entry => entry.Node).OfType<EventSyntax>();

    protected WorkspaceRenameRequest Rename(string before, string after, bool neverPersisted = false) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = WorkspaceSyntaxIndex.Create(Workspace).Entries.First(entry => entry.Node is EventSyntax declaration && declaration.Name == before).Handle,
        ExpectedName = before,
        NewName = after,
        EventNeverPersisted = neverPersisted
    };

    void Establish() => Workspace = Create(Source);
}
