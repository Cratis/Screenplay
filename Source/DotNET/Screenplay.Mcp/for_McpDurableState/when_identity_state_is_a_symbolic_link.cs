// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_identity_state_is_a_symbolic_link : given.a_durable_workspace
{
    Exception _error = null!;
    string _original = string.Empty;

    void Establish()
    {
        _original = Path.Combine(RootPath, "saved-identities.json");
        File.Move(Files.PathFor(McpState.FileName), _original);
        File.CreateSymbolicLink(Path.Combine(RootPath, ".screenplay", McpState.FileName), _original);
    }

    void Because() => _error = Catch.Exception(() => new McpWorkspaces(Root).Open(McpJson.Empty));

    [Fact] void should_not_follow_the_link() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_preserve_the_link_target() => File.ReadAllBytes(_original).AsSpan().SequenceEqual(OriginalState).ShouldBeTrue();
}
