// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAbsenceKeyValidation;

public class when_debt_resolves_without_a_repair : given.two_revisions_of_an_absence_key
{
    Exception? _error;
    IEnumerable<string> _debtBefore = null!;

    void Because()
    {
        // The concept makes the enclosing member's type ambiguous; removing it changes no occurrence of the key's chain.
        var ambiguous = $"concept InvoicePart : String\n{Source}";
        Validate(ambiguous, ambiguous, WorkspaceAuthoringReferencePolicy.Draft);
        _debtBefore = [.. Debt];
        _error = Validate(ambiguous, Source, WorkspaceAuthoringReferencePolicy.Draft);
    }

    [Fact] void should_report_the_member_as_debt_while_its_type_is_ambiguous() => _debtBefore.ShouldContain(message => message.Contains("'part'", StringComparison.Ordinal));
    [Fact] void should_refuse_the_resolution() => _error.ShouldBeOfExactType<InvalidWorkspaceAuthoring>();
    [Fact] void should_explain_the_refusal() => _error!.Message.ShouldContain("Existing absence key debt 'part'");
}
