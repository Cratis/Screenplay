// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_CompletenessChecks;

public class when_selecting_an_unknown_name : Specification
{
    CompletenessChecks _checks;
    bool _accepted;

    void Establish() => _checks = CompletenessChecks.All;
    void Because() => _accepted = CompletenessChecks.TryParse("data-bindings,unknown", out _checks);

    [Fact] void should_reject_the_selection() => _accepted.ShouldBeFalse();
    [Fact] void should_not_leave_a_partial_selection() => _checks.Selected.ShouldBeEmpty();
}
