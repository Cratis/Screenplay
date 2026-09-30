// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAbsenceKeyValidation;

public class when_debt_would_resolve_to_a_declaration_replacing_another : given.two_revisions_of_an_absence_key
{
    Exception? _error;

    void Because()
    {
        // The key member 'segment' is debt; the candidate removes 'part' and adds 'segment', which reads as a rename
        // without the migration that would prove it.
        var debt = Source.Replace("\"part\":\"old\"", "\"segment\":\"old\"", StringComparison.Ordinal);
        _error = Validate(debt, debt.Replace("  part String", "  segment String", StringComparison.Ordinal), WorkspaceAuthoringReferencePolicy.Draft);
    }

    [Fact] void should_refuse_the_captured_debt() => _error.ShouldBeOfExactType<InvalidWorkspaceAuthoring>();
    [Fact] void should_explain_the_refusal() => _error!.Message.ShouldContain("Existing absence key debt 'segment' at");
    [Fact] void should_say_that_it_needs_an_explicit_repair() => _error!.Message.ShouldContain("would become resolved without an explicit repair");
}
