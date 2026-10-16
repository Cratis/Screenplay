// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_matching_date_expectations_ordinally : for_SemanticModelBinder.given.a_semantic_binder
{
    SemanticSpecificationRun _result;

    void Because()
    {
        var bound = Bind("""
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command KeepWindow
                    id String identifier
                    from DateTime
                    to DateTime
                    validate
                      require to == from
                    produces WindowKept
                      for id
                      to = to
                  event WindowKept
                    to DateTime
                  specification ExactSpelling
                    when KeepWindow
                      id = "window"
                      from = "2026-01-01T00:00:00.0000000Z"
                      to = "2026-01-01T01:00:00.0000000+01:00"
                    then WindowKept
                      for "window"
                      to = "2026-01-01T00:00:00.0000000Z"
            """);
        Assert.True(bound.Success, string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(bound.Value!.Model).Plan!;
        _result = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
    }

    [Fact] void should_accept_the_instant_equality_requirement() => _result.Execution.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_not_replace_ordinal_then_matching_with_instant_equality() => _result.Passed.ShouldBeFalse();
}
