// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_discovering_refactorings;

public class and_discovery_writes_nothing : given.a_refactoring_workspace
{
    Dictionary<string, byte[]> _before = [];
    Dictionary<string, byte[]> _after = [];
    JsonElement _response;

    void Establish() => _before = Snapshot();

    void Because()
    {
        _response = Result("read-workspace", new { expectedRevision = CurrentRevision, view = "refactorings" });
        _after = Snapshot();
    }

    [Fact] void should_leave_every_file_under_the_root_unchanged() => _after.Keys.ShouldContainOnly(_before.Keys);
    [Fact] void should_leave_every_byte_unchanged() => _before.All(file => _after[file.Key].SequenceEqual(file.Value)).ShouldBeTrue();
    [Fact] void should_not_retain_a_proposal() => _response.TryGetProperty("proposalId", out _).ShouldBeFalse();

    Dictionary<string, byte[]> Snapshot() => Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
}
