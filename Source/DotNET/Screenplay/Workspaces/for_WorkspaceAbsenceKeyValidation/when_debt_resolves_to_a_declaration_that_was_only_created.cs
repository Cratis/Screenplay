// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAbsenceKeyValidation;

public class when_debt_resolves_to_a_declaration_that_was_only_created : given.two_revisions_of_an_absence_key
{
    Exception? _error;

    void Because()
    {
        // Nothing disappears: 'segment' is added beside 'part', so no rename can have captured the debt.
        var debt = Source.Replace("\"part\":\"old\"", "\"segment\":\"old\"", StringComparison.Ordinal);
        _error = Validate(debt, debt.Replace("  part String", "  part String\n  segment String", StringComparison.Ordinal), WorkspaceAuthoringReferencePolicy.Safe);
    }

    [Fact] void should_accept_the_repair() => _error.ShouldBeNull();
    [Fact] void should_leave_no_debt() => Debt.ShouldBeEmpty();
}
