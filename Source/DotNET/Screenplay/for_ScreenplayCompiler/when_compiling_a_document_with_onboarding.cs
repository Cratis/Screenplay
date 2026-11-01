// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_onboarding : given.a_compiler
{
    const string Source =
        """
        persona Owner
        persona Administrator
        persona Accountant
        persona InvoiceManager

        authentication
          provider EntraId

        onboarding
          signup open creates tenant
          invite tenant as Owner
          invite member
            by Owner, Administrator
            grant Accountant, InvoiceManager
          legal terms
            file Legal/terms-of-service.md
            file Legal/privacy-policy.md
          offering Starter
          emails
            invitation file Emails/invitation.html
            welcome file Emails/welcome.html
            signupDeclined file Emails/signup-declined.html
            images Emails/Images
        """;

    CompilationResult<ApplicationSyntax> _result;
    OnboardingSyntax _onboarding;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _onboarding = _result.Value!.Onboarding!;
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_open_signup_that_creates_a_tenant() => _onboarding.Signup!.Outcome.ShouldEqual(OnboardingSignupOutcome.CreatesTenant);
    [Fact] void should_declare_both_invitations() => _onboarding.Invitations.Select(_ => _.Kind).ShouldContainOnly(OnboardingInvitationKind.Tenant, OnboardingInvitationKind.Member);
    [Fact] void should_grant_the_tenant_persona() => _onboarding.Invitations.Single(_ => _.Kind == OnboardingInvitationKind.Tenant).Grant.ShouldContainOnly("Owner");
    [Fact] void should_say_who_may_invite_members() => _onboarding.Invitations.Single(_ => _.Kind == OnboardingInvitationKind.Member).By.ShouldContainOnly("Owner", "Administrator");
    [Fact] void should_say_what_a_member_invitation_grants() => _onboarding.Invitations.Single(_ => _.Kind == OnboardingInvitationKind.Member).Grant.ShouldContainOnly("Accountant", "InvoiceManager");
    [Fact] void should_keep_the_legal_documents() => _onboarding.LegalTerms!.Documents.Select(_ => _.Path).ShouldContainOnly("Legal/terms-of-service.md", "Legal/privacy-policy.md");
    [Fact] void should_keep_the_offering_as_an_opaque_name() => _onboarding.Offering!.Name.ShouldEqual("Starter");
    [Fact] void should_keep_a_template_per_email() => _onboarding.Emails!.Templates.Select(_ => _.Kind).ShouldContainOnly("invitation", "welcome", "signupDeclined");
    [Fact] void should_keep_the_images_folder() => _onboarding.Emails!.Images!.Path.ShouldEqual("Emails/Images");
}
