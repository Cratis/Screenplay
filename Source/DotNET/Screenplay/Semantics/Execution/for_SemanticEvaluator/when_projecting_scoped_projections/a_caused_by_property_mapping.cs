// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class a_caused_by_property_mapping : given.a_scoped_projection_plan
{
    [Theory]
    [InlineData("subject", "kari-subject")]
    [InlineData("name", "Kari Nordmann")]
    [InlineData("userName", "kari")]
    void should_map_the_caller_identity_recorded_with_the_fact(string member, string expected)
    {
        Plan($"from OrderShipped\n  label = $eventContext.causedBy.{member}");
        _compilation.Issues.ShouldBeEmpty();
        Project(Fact("OrderShipped", FirstOrder) with { CausedBy = new("kari-subject", "Kari Nordmann", "kari") });
        _failure.ShouldBeNull();
        SemanticValueRules.AreEqual(Value(FirstOrder, "label"), Text(expected)).ShouldBeTrue();
    }

    [Fact]
    void should_keep_the_first_identity_with_a_set_once_mapping()
    {
        Plan("from OrderShipped\n  set once label = $eventContext.causedBy.subject");
        _compilation.Issues.ShouldBeEmpty();
        Project(
            Fact("OrderShipped", FirstOrder) with { CausedBy = new("first", "First", "first") },
            Fact("OrderShipped", FirstOrder) with { CausedBy = new("second", "Second", "second") });
        _failure.ShouldBeNull();
        SemanticValueRules.AreEqual(Value(FirstOrder, "label"), Text("first")).ShouldBeTrue();
    }

    [Fact]
    void should_fail_when_the_fact_carries_no_caller_identity()
    {
        Plan("from OrderShipped\n  label = $eventContext.causedBy.subject");
        _compilation.Issues.ShouldBeEmpty();
        Project(Fact("OrderShipped", FirstOrder));
        _failure.ShouldContain("caller identity");
    }
}
