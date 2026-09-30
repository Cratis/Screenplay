// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAbsenceKeyValidation;

public class when_removing_a_duplicate_key_declaration : given.two_revisions_of_an_absence_key
{
    Exception? _error;
    IEnumerable<string> _debtBefore = null!;

    void Because()
    {
        var duplicated = Source.Replace("  part String", "  part String\n  part String", StringComparison.Ordinal);
        Validate(duplicated, duplicated, WorkspaceAuthoringReferencePolicy.Draft);
        _debtBefore = [.. Debt];
        Diagnostics.Clear();
        _error = Validate(duplicated, Source, WorkspaceAuthoringReferencePolicy.Safe);
    }

    [Fact] void should_report_the_member_as_debt_while_it_is_declared_twice() => _debtBefore.Single().ShouldContain("declares property 'part' more than once");
    [Fact] void should_accept_the_removal_as_an_explicit_repair() => _error.ShouldBeNull();
    [Fact] void should_leave_no_debt() => Debt.ShouldBeEmpty();
}
