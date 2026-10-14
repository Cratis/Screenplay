// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_PersistedIdentityStateSerializer.given;

public class a_persisted_workspace : Specification
{
    protected ScreenplayWorkspace Workspace = null!;
    protected byte[] Bytes = [];

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = WorkspaceDocument.Create(catalog.ResolveDocument("application"), "application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes("""
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  event ProjectRegistered
                    name String
            """));
        Workspace = ScreenplayWorkspace.Create("Projects", [document], catalog);
        Bytes = PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(Workspace));
    }
}
