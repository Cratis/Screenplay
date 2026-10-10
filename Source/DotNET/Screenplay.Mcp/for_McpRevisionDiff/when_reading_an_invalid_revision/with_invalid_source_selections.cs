// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff.when_reading_an_invalid_revision;

public class with_invalid_source_selections : given.two_snapshots
{
    List<Exception?> _failures = [];

    void Because()
    {
        foreach (var source in new object[] { new { }, new { workspace = "other" }, new { workspace = "active", path = "." }, new { path = ".", workspaceJson = BeforeJson }, new { workspaceJson = BeforeJson, workspace = "active" }, "active", new { @ref = "main" } })
        {
            _failures.Add(Catch.Exception(() => Call(new { before = source, afterWorkspaceJson = AfterJson })));
            _failures.Add(Catch.Exception(() => Call(new { beforeWorkspaceJson = BeforeJson, after = source })));
        }

        _failures.Add(Catch.Exception(() => Call(new { before = new { workspaceJson = BeforeJson }, beforeWorkspaceJson = BeforeJson, afterWorkspaceJson = AfterJson })));
        _failures.Add(Catch.Exception(() => Call(new { beforeWorkspaceJson = BeforeJson, after = new { workspaceJson = AfterJson }, afterWorkspaceJson = AfterJson })));
        _failures.Add(Catch.Exception(() => Call(new { beforeWorkspaceJson = BeforeJson })));
        _failures.Add(Catch.Exception(() => Call(new { afterWorkspaceJson = AfterJson })));
    }

    [Fact] void should_refuse_every_invalid_selection() => _failures.TrueForAll(failure => failure is McpFailure { Code: -32602 }).ShouldBeTrue();
}
