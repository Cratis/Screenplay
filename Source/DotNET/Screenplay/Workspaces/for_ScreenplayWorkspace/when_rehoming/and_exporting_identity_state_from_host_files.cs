// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_exporting_identity_state_from_host_files : given.a_workspace_with_assigned_events
{
    PersistedIdentityState _state = null!;

    void Because() => _state = PersistedIdentityStateSerializer.Deserialize(IdentityStateFor(
        Original, [.. Split.Select(document => (document.Path.Value, document.Bytes.ToArray()))]));

    [Fact] void should_export_the_host_event_assignments() => _state.IdentityCatalog.EventContracts.ShouldContainOnly(Original.IdentityCatalog.EventContracts);
    [Fact] void should_export_the_new_document_paths() => _state.Documents.Select(document => document.Path).ShouldContainOnly(Split.Select(document => document.Path));

    static byte[] IdentityStateFor(
        ScreenplayWorkspace workspace,
        ImmutableArray<(string Path, byte[] Bytes)> files)
    {
        var documents = files.Select(file =>
        {
            var path = PortablePlayPath.Parse(file.Path);
            return WorkspaceDocument.Create(ModelRootDocumentKeys.For(path), path, file.Bytes);
        }).ToImmutableArray();
        var rehomed = workspace.Rehome(documents);
        return PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(rehomed));
    }
}
