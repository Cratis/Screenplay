// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer;

public class when_a_supplied_catalog_would_be_materialized : given.a_transport_workspace
{
    byte[] _bytes = null!;
    Exception? _error;

    void Establish()
    {
        var incomplete = SemanticIdentityCatalog.Create(
            StableApplicationIdentity,
            Workspace.IdentityCatalog.Documents,
            [.. Workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Id != CommandSemanticIdentity)],
            Workspace.IdentityCatalog.EventContracts);
        var root = JsonNode.Parse(ScreenplayWorkspaceSerializer.Serialize(Workspace))!.AsObject();
        root["identityCatalog"] = JsonNode.Parse(SemanticIdentityCatalogSerializer.Serialize(incomplete));
        root["revision"] = WorkspaceCanonicalRevision.Compute(Workspace.ApplicationName, Workspace.Documents, incomplete).ToString();
        _bytes = Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    void Because() => _error = Catch.Exception(() => ScreenplayWorkspaceSerializer.Deserialize(_bytes));

    [Fact] void should_reject_instead_of_rebootstrap() => _error.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
    [Fact] void should_report_the_authoritative_catalog_change() => _error!.Message.ShouldEqual("Workspace admission would change the supplied authoritative identity catalog.");
}
