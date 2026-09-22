// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_a_mapped_document_is_moved_externally : given.a_durable_workspace
{
    Exception _error = null!;

    void Establish() => File.Move(Path.Combine(RootPath, "application.play"), Path.Combine(RootPath, "external.play"));
    void Because() => _error = Catch.Exception(() => new McpWorkspaces(Root).Open(McpJson.Empty));

    [Fact] void should_require_explicit_reconciliation() => _error.Message.Contains("IdentityMappingConflict", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_replace_identity_state() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
}
