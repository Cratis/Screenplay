// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_expected_examples_change : given.a_semantic_comparison
{
    const string Model = """
        module M
          feature F
            slice StateChange S
              event E
                name String
              command C
                name String
                produces E
                  name = name
              readmodel R
                name String
              example Expected : E
                name = "First"
              example State : R
                name = "First"
              specification Inherited
                when C
                  name = "First"
                then Expected
              specification Overridden
                when C
                  name = "Second"
                then Expected name = "Second"
              specification ReadModel
                given readmodel State
                when C
                  name = "First"
                then readmodel State
        """;

    void Because()
    {
        var after = Model.Replace("example Expected : E\n        name = \"First\"", "example Expected : E\n        name = \"Changed\"", StringComparison.Ordinal)
            .Replace("example State : R\n        name = \"First\"", "example State : R\n        name = \"Changed\"", StringComparison.Ordinal);
        CompareSnapshots(Model, after);
    }

    [Fact] void should_report_the_effective_event_expectation_change() => Items("specifications").Single(item => item.GetProperty("afterAddress").GetString() == "M.F.S.Inherited").GetProperty("changeKind").GetString().ShouldEqual("expected-outcome-changed");
    [Fact] void should_name_the_event_assertion_member() => Items("specifications").Single(item => item.GetProperty("afterAddress").GetString() == "M.F.S.Inherited").GetProperty("member").GetString().ShouldEqual("thenEvents");
    [Fact] void should_report_the_effective_read_model_expectation_change() => Items("specifications").Single(item => item.GetProperty("afterAddress").GetString() == "M.F.S.ReadModel").GetProperty("member").GetString().ShouldEqual("thenReadModels");
    [Fact] void should_not_report_an_overridden_example_as_an_outcome_change() => Items("specifications").Any(item => item.GetProperty("afterAddress").GetString() == "M.F.S.Overridden").ShouldBeFalse();
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
