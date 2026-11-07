// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_CompletenessChecks;

/// <summary>
/// PLAY0533 is on by default since Screenplay 6. A selection written for Screenplay 5 that names it keeps working and
/// still selects the input-surfaces check it selected then.
/// </summary>
public class when_selecting_the_check_that_is_on_by_default : Specification
{
    CompletenessChecks _checks;
    bool _accepted;

    void Because() => _accepted = CompletenessChecks.TryParse("PLAY0533", out _checks);

    [Fact] void should_accept_the_code() => _accepted.ShouldBeTrue();
    [Fact] void should_select_what_it_selected_in_screenplay_5() => _checks.Selected.ShouldContainOnly(CompletenessCheck.InputSurfaces);
    [Fact] void should_list_it_as_an_accepted_name() => CompletenessChecks.AcceptedNames.ShouldContain("PLAY0533");
    [Fact] void should_keep_the_check_name() => CompletenessChecks.NameOf(CompletenessCheck.InputSurfaces).ShouldEqual("input-surfaces");
}
