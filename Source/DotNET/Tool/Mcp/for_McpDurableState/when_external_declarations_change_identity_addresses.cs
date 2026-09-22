// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_external_declarations_change_identity_addresses : given.a_durable_workspace
{
    Exception _error = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("Registration", "Enrollment", StringComparison.Ordinal));
    void Because() => _error = Catch.Exception(() => new McpWorkspaces(Root).Open(McpJson.Empty));

    [Fact] void should_require_explicit_identity_reconciliation() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_not_replace_the_catalog() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
}
