// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_PersonaCompleteness;

public class when_evaluating_policy_conditions : Specification
{
    const string Source = """
        policy Member
          require role "A"
        policy Restricted
          require REQUIREMENT
        persona Person
          policy Member
        module M
          feature F
            slice StateChange S
              command C
                authorize Restricted
        """;

    [Fact] void should_deny_when_a_role_is_missing() => IsDenied("role \"Other\"").ShouldBeTrue();
    [Fact] void should_deny_when_a_false_left_meets_an_unknown_right_in_an_and() => IsDenied("role \"Other\" and claim \"owner\" matches subject").ShouldBeTrue();
    [Fact] void should_deny_when_an_unknown_left_meets_a_false_right_in_an_and() => IsDenied("claim \"owner\" matches subject and role \"Other\"").ShouldBeTrue();
    [Fact] void should_not_deny_when_an_unknown_meets_a_true_in_an_and() => IsDenied("claim \"owner\" matches subject and role \"A\"").ShouldBeFalse();
    [Fact] void should_not_deny_when_a_false_left_meets_an_unknown_right_in_an_or() => IsDenied("role \"Other\" or claim \"owner\" matches subject").ShouldBeFalse();
    [Fact] void should_not_deny_when_an_unknown_left_meets_a_false_right_in_an_or() => IsDenied("claim \"owner\" matches subject or role \"Other\"").ShouldBeFalse();
    [Fact] void should_not_deny_when_a_true_meets_an_unknown_in_an_or() => IsDenied("role \"A\" or claim \"owner\" matches subject").ShouldBeFalse();
    [Fact] void should_deny_when_both_sides_of_an_or_are_false() => IsDenied("role \"Other\" or role \"Another\"").ShouldBeTrue();
    [Fact] void should_deny_a_negated_true() => IsDenied("not role \"A\"").ShouldBeTrue();
    [Fact] void should_keep_a_negated_unknown_unknown() => IsDenied("not claim \"owner\" matches subject").ShouldBeFalse();
    [Fact] void should_deny_a_literal_claim_the_caller_does_not_hold() => IsDenied("claim \"owner\" matches \"x\"").ShouldBeTrue();
    [Fact] void should_not_deny_an_anonymous_condition() => IsDenied("anonymous").ShouldBeFalse();

    static bool IsDenied(string requirement)
    {
        CompletenessChecks.TryParse("personas", out var checks).ShouldBeTrue();
        var findings = ModelCompleteness.Check(new ScreenplayCompiler().Compile(Source.Replace("REQUIREMENT", requirement, StringComparison.Ordinal)), checks);

        return findings.Any(diagnostic => diagnostic.Code == DiagnosticCodes.GateWithoutPersona && diagnostic.Message.Contains("Command 'C'", StringComparison.Ordinal));
    }
}
