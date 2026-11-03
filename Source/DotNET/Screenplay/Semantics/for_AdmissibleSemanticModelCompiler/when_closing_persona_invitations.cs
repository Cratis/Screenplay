// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_closing_persona_invitations : given.persona_documents
{
    const string Onboarding = "authentication\n  provider EntraId\nonboarding\n  signup open creates tenant\nINVITATIONS\n  legal terms\n    file Legal/terms.md\n  offering Starter\n  emails\n    invitation file Emails/invitation.html\n    images Emails/Images\n";

    [Theory]
    [InlineData("Clerk", "Healthy")]
    [InlineData("Healthy", "Clerk")]
    [InlineData("Clerk", "Clerk")]
    [InlineData("Healthy, Clerk", "Clerk, Healthy")]
    void should_remove_the_whole_member_invitation(string by, string grant)
    {
        Compile($"  invite tenant as Healthy\n  invite member\n    by {by}\n    grant {grant}");
        _rounds[^1].Onboarding!.Invitations.Select(invitation => invitation.Kind).ShouldEqual([OnboardingInvitationKind.Tenant]);
        _rounds[^1].Onboarding!.Invitations.Single().ShouldEqual(_rounds[0].Onboarding!.Invitations.First());
        PolicyExclusion().Reason.ShouldContain("Member invitation at main.play:");
        AssertSurvivors();
    }

    [Fact]
    void should_remove_the_whole_tenant_invitation()
    {
        Compile("  invite tenant as Clerk\n  invite member\n    by Healthy\n    grant Healthy");
        _rounds[^1].Onboarding!.Invitations.Select(invitation => invitation.Kind).ShouldEqual([OnboardingInvitationKind.Member]);
        _rounds[^1].Onboarding!.Invitations.Single().ShouldEqual(_rounds[0].Onboarding!.Invitations.Last());
        PolicyExclusion().Reason.ShouldContain("Tenant invitation at main.play:");
        AssertSurvivors();
    }

    [Fact]
    void should_report_multiple_whole_personas_once_in_source_order()
    {
        Source(Policy + Personas + "persona Second\n  policy Access\n" + Authorized);
        Record();
        Admit();
        _result.Compilation.Success.ShouldBeTrue();
        _rounds[^1].Personas!.Select(persona => persona.Name).ShouldEqual(["Healthy"]);
        var reason = PolicyExclusion().Reason!;
        reason.IndexOf("persona 'Clerk'", StringComparison.Ordinal).ShouldBeLessThan(reason.IndexOf("persona 'Second'", StringComparison.Ordinal));
        reason.Split("persona 'Clerk'", StringSplitOptions.None).Length.ShouldEqual(2);
    }

    [Fact]
    void should_preserve_absent_metadata()
    {
        Source(Policy + Valid);
        Record();
        Admit();
        _result.Compilation.Success.ShouldBeTrue();
        _rounds[^1].Personas.ShouldEqual(_rounds[0].Personas);
        _rounds[^1].Onboarding.ShouldBeNull();
    }

    void Admit() => _result = _compiler.CompileAdmissible("Example", _documents);

    void Compile(string invitations)
    {
        Source(Policy + Personas + Onboarding.Replace("INVITATIONS", invitations, StringComparison.Ordinal) + Authorized);
        new ScreenplayCompiler().Compile(_documents.Documents.Single().Text).Success.ShouldBeTrue();
        Record();
        _result = _compiler.CompileAdmissible("Example", _documents);
    }

    SemanticExclusion PolicyExclusion() => _result.Exclusions.Single(exclusion => exclusion.Kind == SemanticExclusionKind.Policy);

    void AssertSurvivors()
    {
        _normal.Success.ShouldBeFalse();
        _result.Compilation.Success.ShouldBeTrue();
        _rounds.Count.ShouldEqual(2);
        var original = _rounds[0];
        var reduced = _rounds[^1];
        reduced.Personas!.Single().ShouldEqual(original.Personas!.Single(persona => persona.Name == "Healthy"));
        reduced.Personas!.Single().Policies.ShouldEqual(["Extra", "Member"]);
        reduced.Onboarding!.ShouldEqual(original.Onboarding! with { Invitations = reduced.Onboarding.Invitations });
        new ScreenplayCompiler().Compile(new ScreenplayPrinter().Print(reduced)).Success.ShouldBeTrue();
        _documents.Documents.Single().Text.ShouldContain("persona Clerk\n  policy Member\n  policy Access");
        PolicyExclusion().Diagnostics.ShouldNotBeEmpty();
    }
}
