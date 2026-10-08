// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_CompletenessChecks;

public class when_selecting_checks : Specification
{
    CompletenessChecks _checks;
    bool _accepted;

    void Establish() => _checks = CompletenessChecks.None;
    void Because() => _accepted = CompletenessChecks.TryParse("data-bindings, PLAY0537, data-bindings", out _checks);

    [Fact] void should_accept_names_and_codes() => _accepted.ShouldBeTrue();
    [Fact] void should_combine_without_duplicates() => _checks.Selected.ShouldContainOnly(CompletenessCheck.DataBindings, CompletenessCheck.Navigation);
}
