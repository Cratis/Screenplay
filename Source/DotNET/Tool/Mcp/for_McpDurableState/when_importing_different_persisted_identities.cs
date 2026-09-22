// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_importing_different_persisted_identities : given.a_durable_workspace
{
    Exception _error = null!;

    void Because()
    {
        var other = ScreenplayWorkspace.Create("Other", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Other")));
        var serialized = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(other));
        _error = Catch.Exception(() => new McpWorkspaces(Root).Open(Arguments(new { workspaceJson = serialized })));
    }

    [Fact] void should_refuse_silent_identity_replacement() => _error.Message.Contains("IdentityImportConflict", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_persisted_catalog() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
}
