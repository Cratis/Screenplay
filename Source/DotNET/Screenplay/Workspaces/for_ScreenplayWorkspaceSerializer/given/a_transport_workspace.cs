// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer.given;

public class a_transport_workspace : for_ScreenplayWorkspace.given.a_workspace_with_external_identities
{
    void Establish()
    {
        Concepts = WorkspaceDocument.Create(
            DocumentId.Create("persisted-concepts-42"),
            Concepts.StableKey,
            Concepts.Path,
            [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(ConceptsSource.Replace("\n", "\r\n", StringComparison.Ordinal) + "\r\n")]);
        Registration = WorkspaceDocument.Create(
            DocumentId.Create("persisted-registration-42"),
            Registration.StableKey,
            Registration.Path,
            Registration.Bytes.AsSpan());
        var catalog = SemanticIdentityCatalog.Create(
            StableApplicationIdentity,
            [
                new(Concepts.StableKey, Concepts.Id, SemanticIdentityOrigin.Persisted),
                new(Registration.StableKey, Registration.Id, SemanticIdentityOrigin.Persisted)
            ],
            Workspace.IdentityCatalog.Semantics,
            Workspace.IdentityCatalog.EventContracts);
        Workspace = ScreenplayWorkspace.Create(StableApplicationIdentity, Workspace.ApplicationName, [Registration, Concepts], catalog);
    }
}
