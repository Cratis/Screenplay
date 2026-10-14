// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_a_new_key_has_a_host_document_id : given.a_workspace_with_assigned_events
{
    ScreenplayWorkspace _rehomed = null!;

    void Because() => _rehomed = Original.Rehome([WorkspaceDocument.Create(Original.Documents.Single().Id, Split[0].StableKey, Split[0].Path, Split[0].Bytes.AsSpan()), Split[1]]);

    [Fact] void should_rebind_the_new_key_to_its_bootstrap_id() => _rehomed.Documents.Single(document => document.StableKey == Split[0].StableKey).Id.ShouldEqual(DocumentId.Create(Split[0].StableKey));
}
