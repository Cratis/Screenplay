// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_a_declaration_changes_owner : given.a_semantic_comparison
{
    JsonElement _move;

    void Because()
    {
        const string before = """
            policy Access
              require authenticated
            module Projects
              feature Restricted
                authorize Access
                slice StateChange Left
                  command Run
              feature Open
                slice StateChange Right
                  command Other
            """;
        var after = before.Replace("      command Run\n", string.Empty, StringComparison.Ordinal) + "\n      command Run\n";
        CompareSnapshots(before, after, catalog => SemanticIdentityCatalog.Create(
            catalog.Application,
            catalog.Documents,
            [.. catalog.Semantics.Select(assignment => assignment.Address.Kind == SemanticKind.Command && assignment.Address.Name == "Run"
                ? assignment with { Address = SemanticAddress.ForCommand(SemanticAddress.ForSlice(catalog.Application, "Projects", "Open", "Right"), "Run"), Origin = SemanticIdentityOrigin.Persisted }
                : assignment)],
            catalog.EventContracts));
        _move = Items("declarations").Single(item => item.GetProperty("kind").GetString() == "Command" && item.GetProperty("changeKind").GetString() == "moved");
    }

    [Fact] void should_distinguish_semantic_owner_movement_from_layout() => _move.GetProperty("moveKind").GetString().ShouldEqual("owner");
    [Fact] void should_report_the_previous_owner() => _move.GetProperty("beforeOwner").GetString().ShouldEqual("Projects.Restricted.Left");
    [Fact] void should_report_the_new_owner() => _move.GetProperty("afterOwner").GetString().ShouldEqual("Projects.Open.Right");
    [Fact] void should_report_a_known_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
