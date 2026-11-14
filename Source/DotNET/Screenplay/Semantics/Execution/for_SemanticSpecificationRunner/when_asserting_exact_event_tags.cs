// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_asserting_exact_event_tags : given.a_plan_with_tag_expectations
{
    bool _absent;
    bool _empty;
    bool _exact;
    bool _missing;
    bool _extra;
    bool _reordered;
    bool _duplicate;
    bool _taggedWhenEmpty;

    void Because()
    {
        _absent = Passes([["audit", "billing"]], [null]);
        _empty = Passes([[]], [[]]);
        _exact = Passes([["audit", "billing", "audit"]], [["audit", "billing", "audit"]]);
        _missing = Passes([["audit"]], [["audit", "billing"]]);
        _extra = Passes([["audit", "billing"]], [["audit"]]);
        _reordered = Passes([["billing", "audit"]], [["audit", "billing"]]);
        _duplicate = Passes([["audit", "audit"]], [["audit"]]);
        _taggedWhenEmpty = Passes([["audit"]], [[]]);
    }

    [Fact] void should_leave_absent_tags_unchecked() => _absent.ShouldBeTrue();
    [Fact] void should_accept_no_tags_on_an_untagged_fact() => _empty.ShouldBeTrue();
    [Fact] void should_preserve_exact_order_and_duplicates() => _exact.ShouldBeTrue();
    [Fact] void should_reject_a_missing_tag() => _missing.ShouldBeFalse();
    [Fact] void should_reject_an_extra_tag() => _extra.ShouldBeFalse();
    [Fact] void should_reject_reordered_tags() => _reordered.ShouldBeFalse();
    [Fact] void should_reject_an_extra_duplicate() => _duplicate.ShouldBeFalse();
    [Fact] void should_reject_tags_when_none_are_expected() => _taggedWhenEmpty.ShouldBeFalse();
}
