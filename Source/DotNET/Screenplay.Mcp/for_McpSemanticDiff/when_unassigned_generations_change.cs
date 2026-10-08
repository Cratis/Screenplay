// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_unassigned_generations_change : given.a_semantic_comparison
{
    void Because()
    {
        const string source = """
            system Store
            module Projects
              feature Registration
                slice StateChange Register
                  operation Save
                    uses Store
                  event Registered
                    name String
                  event Registered generation 2
                    name String
                    extra String
            """;
        CompareSnapshots(source, source.Replace("extra String", "extra Bool", StringComparison.Ordinal));
    }

    [Fact] void should_compare_the_unassigned_second_generation() => Items("events").Single(item => item.GetProperty("changeKind").GetString() == "property-type-changed").GetProperty("afterGeneration").GetUInt32().ShouldEqual(2u);
    [Fact] void should_not_fabricate_a_contract_identity() => Items("events").Single().GetProperty("semanticId").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_disclose_incomplete_event_identity_comparison() => Section("events").GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_known_contract_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
