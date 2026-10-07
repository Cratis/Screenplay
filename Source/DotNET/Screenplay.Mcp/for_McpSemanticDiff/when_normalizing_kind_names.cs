// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_normalizing_kind_names : given.a_semantic_comparison
{
    static readonly string[] _aliases = ["CompositeType", "EventContract", "EventContractIdentity"];
    JsonElement[] _assigned = [];
    JsonElement[] _fallback = [];
    bool _hasAssignedType;

    void Because()
    {
        const string before = """
            type Details
              name String
            policy Access
              require authenticated
            module Projects
              feature Registration
                slice StateChange Register
                  command Register
                    details Details
                    authorize Access
                  event Registered
                    name String
            """;
        var after = before.Replace("  name String", "  name Bool", StringComparison.Ordinal) + "\n      event Extra\n        name String\n";
        CompareSnapshots(before, after);
        _hasAssignedType = Workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Kind == SemanticKind.CompositeType);
        _assigned = [.. Diff.GetProperty("page").GetProperty("items").EnumerateArray()];
        const string operation = "\n      operation Save\n        uses Store\n";
        CompareSnapshots("system Store\n" + before + operation, "system Store\n" + after + operation);
        _fallback = [.. Diff.GetProperty("page").GetProperty("items").EnumerateArray()];
    }

    [Fact] void should_exercise_an_assigned_composite_type() => _hasAssignedType.ShouldBeTrue();
    [Fact] void should_use_type_for_assigned_type_members() => _assigned.Any(item => item.GetProperty("section").GetString() == "members" && item.GetProperty("kind").GetString() == "Type" && item.GetProperty("semanticId").ValueKind == JsonValueKind.String).ShouldBeTrue();
    [Fact] void should_use_type_for_fallback_type_members() => _fallback.Any(item => item.GetProperty("section").GetString() == "members" && item.GetProperty("kind").GetString() == "Type" && item.GetProperty("semanticId").ValueKind == JsonValueKind.Null).ShouldBeTrue();
    [Fact] void should_use_type_for_type_dependants() => _assigned.Any(item => item.GetProperty("section").GetString() == "dependants" && item.GetProperty("kind").GetString() == "Type").ShouldBeTrue();
    [Fact] void should_use_event_for_event_identity_records() => _assigned.Any(item => item.GetProperty("section").GetString() == "identities" && item.GetProperty("eventContractId").ValueKind == JsonValueKind.String && item.GetProperty("kind").GetString() == "Event").ShouldBeTrue();
    [Fact] void should_not_emit_alternative_kind_spellings() => _assigned.Concat(_fallback).Any(item => _aliases.Contains(item.GetProperty("kind").GetString(), StringComparer.Ordinal)).ShouldBeFalse();
}
