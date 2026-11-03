// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_closing_personas_across_rounds : given.persona_documents
{
    void Establish()
    {
        Source("policy Access\n  require role \"Access\"\npolicy SecondAccess\n  require role \"SecondAccess\"\n" + Personas + "persona Second\n  policy SecondAccess\nauthentication\n  provider EntraId\nonboarding\n  invite tenant as Healthy\n  invite member\n    by Clerk\n    grant Second\n" + Valid);
        _normal.Success.ShouldBeTrue();
        Record((syntax, round) => round <= 2 ? Failure(syntax, round == 1 ? "Access" : "SecondAccess") : null);
    }

    static CompilationResult<SemanticCompilation> Failure(ApplicationSyntax syntax, string policy) =>
        CompilationResult<SemanticCompilation>.Failed([PolicyError("Requires exclusion.", syntax.Policies.Single(value => value.Name == policy).Location, policy)]);

    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_complete_the_second_closure() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_bind_each_progressive_subset_once() => _rounds.Count.ShouldEqual(3);
    [Fact] void should_remove_only_the_first_persona_in_the_first_round() => _rounds[1].Personas!.Select(persona => persona.Name).ShouldEqual(["Healthy", "Second"]);
    [Fact] void should_keep_only_the_healthy_persona() => _rounds[2].Personas!.Select(persona => persona.Name).ShouldEqual(["Healthy"]);
    [Fact] void should_keep_the_healthy_policy_order() => _rounds[2].Personas!.Single().Policies.ShouldEqual(["Extra", "Member"]);
    [Fact] void should_remove_the_whole_cross_persona_invitation_once() => _rounds.Skip(1).ShouldEachConformTo(syntax => syntax.Onboarding!.Invitations.Count() == 1);
    [Fact] void should_order_policy_evidence_by_source() => _result.Exclusions.Select(exclusion => exclusion.Address).ShouldEqual(["Access", "SecondAccess"]);
    [Fact] void should_name_the_second_persona() => _result.Exclusions[1].Reason.ShouldContain("persona 'Second' at main.play:");
}
