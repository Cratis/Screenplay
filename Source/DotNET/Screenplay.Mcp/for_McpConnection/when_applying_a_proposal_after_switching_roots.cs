// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_applying_a_proposal_after_switching_roots : given.a_worktree_connection
{
    JsonElement _first;
    JsonElement _second;
    JsonElement _refused;
    string _proposalId;
    void Establish()
    {
        _first = Open();
        _proposalId = ProposeMove(_first).GetProperty("structuredContent").GetProperty("proposalId").GetString()!;
        _second = Open(WorktreeModelPath);
    }
    void Because() => _refused = ApplyMove(_second, _proposalId);
    [Fact] void should_exercise_equal_revisions_in_distinct_roots() => _second.GetProperty("revision").GetString().ShouldEqual(_first.GetProperty("revision").GetString());
    [Fact] void should_refuse_the_previous_roots_proposal() => _refused.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("UnknownProposal");
    [Fact] void should_leave_both_roots_unchanged() => new[] { ModelPath, WorktreeModelPath }.All(path => File.ReadAllText(Path.Combine(path, "application.play")) == Source && !File.Exists(Path.Combine(path, "renamed.play"))).ShouldBeTrue();
}
