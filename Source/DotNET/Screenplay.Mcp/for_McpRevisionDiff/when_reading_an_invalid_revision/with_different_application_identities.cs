// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff.when_reading_an_invalid_revision;

public class with_different_application_identities : given.two_snapshots
{
    JsonElement _result;

    void Because()
    {
        var unrelated = ScreenplayWorkspace.Create("Other", Proposal.Workspace.Documents, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Other")));
        _result = Call(new { beforeWorkspaceJson = BeforeJson, afterWorkspaceJson = Export(unrelated) });
    }

    [Fact] void should_refuse_to_guess_identity_continuity() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_return_a_structured_failure() => _result.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("IncompatibleRevisions");
}
