// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_CompletenessChecks;

public class when_selecting_all : Specification
{
    CompletenessChecks _checks;
    bool _accepted;

    void Establish() => _checks = CompletenessChecks.None;
    void Because() => _accepted = CompletenessChecks.TryParse("all", out _checks);

    [Fact] void should_accept_all() => _accepted.ShouldBeTrue();
    [Fact] void should_select_every_check() => _checks.Selected.ShouldContainOnly(Enum.GetValues<CompletenessCheck>());
}
