// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_matching_tagged_facts_in_any_order : given.a_plan_with_tag_expectations
{
    bool _reversed;
    bool _reassigned;
    bool _reused;
    bool _ordered;

    void Because()
    {
        _reversed = Passes([["audit"], ["billing"]], [["billing"], ["audit"]], true);
        _reassigned = Passes([["audit"], ["billing"]], [null, ["audit"]], true);
        _reused = Passes([["audit"], ["billing"]], [["audit"], ["audit"]], true);
        _ordered = Passes([["audit"], ["billing"]], [["billing"], ["audit"]]);
    }

    [Fact] void should_distinguish_identical_payloads_by_tags() => _reversed.ShouldBeTrue();
    [Fact] void should_reassign_a_wildcard_to_leave_the_exact_match_available() => _reassigned.ShouldBeTrue();
    [Fact] void should_not_match_the_same_fact_twice() => _reused.ShouldBeFalse();
    [Fact] void should_still_require_order_without_the_qualifier() => _ordered.ShouldBeFalse();
}
