// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    void WriteOnboarding(ScreenplayWriter writer, OnboardingSyntax onboarding)
    {
        using var anchor = writer.Anchor(onboarding);
        writer.Line("onboarding");
        using (writer.Indent())
        {
            if (onboarding.Signup is { } signup)
            {
                var outcome = signup.Outcome == OnboardingSignupOutcome.CreatesTenant ? "creates" : "joins";
                writer.Line($"signup open {outcome} tenant", signup);
            }

            foreach (var invitation in onboarding.Invitations)
            {
                WriteInvitation(writer, invitation);
            }

            if (onboarding.LegalTerms is { } terms)
            {
                using var termsAnchor = writer.Anchor(terms);
                writer.Line("legal terms");
                using (writer.Indent())
                {
                    foreach (var document in terms.Documents)
                    {
                        WriteFile(writer, document);
                    }
                }
            }

            if (onboarding.Offering is { } offering)
            {
                writer.Line($"offering {offering.Name}", offering);
            }

            if (onboarding.Emails is { } emails)
            {
                WriteEmails(writer, emails);
            }
        }
    }

    static void WriteInvitation(ScreenplayWriter writer, OnboardingInvitationSyntax invitation)
    {
        if (invitation.Kind == OnboardingInvitationKind.Tenant)
        {
            writer.Line($"invite tenant as {string.Join(", ", invitation.Grant)}", invitation);
            return;
        }

        using var anchor = writer.Anchor(invitation);
        writer.Line("invite member");
        using (writer.Indent())
        {
            writer.Line($"by {string.Join(", ", invitation.By)}");
            writer.Line($"grant {string.Join(", ", invitation.Grant)}");
        }
    }

    static void WriteEmails(ScreenplayWriter writer, OnboardingEmailsSyntax emails)
    {
        using var anchor = writer.Anchor(emails);
        writer.Line("emails");
        using (writer.Indent())
        {
            foreach (var template in emails.Templates)
            {
                writer.Line($"{template.Kind} file {template.File.Path}", template);
            }

            if (emails.Images is { } images)
            {
                writer.Line($"images {images.Path}", images);
            }
        }
    }
}
