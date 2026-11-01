// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses top level <c>onboarding</c> blocks - how people sign up, are invited and accept legal terms.
/// </summary>
internal static partial class OnboardingParser
{
    const string Personas = @"[A-Za-z_]\w*(?:\s*,\s*[A-Za-z_]\w*)*";

    /// <summary>
    /// Parses an onboarding block from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>onboarding</c> header.</param>
    /// <param name="existing">The onboarding block already parsed for the document, when there is one.</param>
    /// <returns>The parsed <see cref="OnboardingSyntax"/>, or <paramref name="existing"/> when the block is invalid or a duplicate.</returns>
    public static OnboardingSyntax? Parse(ParserContext context, SourceLine header, OnboardingSyntax? existing)
    {
        if (header.Content != "onboarding")
        {
            context.Error(DiagnosticCodes.InvalidOnboardingDeclaration, $"Invalid onboarding declaration '{header.Content}' - expected 'onboarding'", header.Location);
            context.SkipBlock(header.Indent);
            return existing;
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateOnboarding, "The document already declares an onboarding block - an application can have at most one", header.Location);
            context.SkipBlock(header.Indent);
            return existing;
        }

        var state = new State();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "signup":
                    ParseSignup(context, line, state);
                    break;
                case "invite":
                    ParseInvitation(context, line, state);
                    break;
                case "legal":
                    ParseLegalTerms(context, line, state);
                    break;
                case "offering":
                    ParseOffering(context, line, state);
                    break;
                case "emails":
                    ParseEmails(context, line, state);
                    break;
                default:
                    context.Error(
                        DiagnosticCodes.InvalidOnboardingDirective,
                        $"Unexpected '{line.Content}' in an onboarding block - expected signup, invite, legal terms, offering or emails",
                        line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new OnboardingSyntax(header.Location)
        {
            Signup = state.Signup,
            Invitations = state.Invitations,
            LegalTerms = state.LegalTerms,
            Offering = state.Offering,
            Emails = state.Emails
        };
    }

    static void ParseSignup(ParserContext context, SourceLine line, State state)
    {
        var match = SignupRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Invalid signup '{line.Content}' - expected 'signup open creates tenant' or 'signup open joins tenant'", line.Location);
        }
        else if (state.Signup is not null)
        {
            Duplicate(context, "signup", line);
        }
        else
        {
            var outcome = match.Groups[1].Value == "creates" ? OnboardingSignupOutcome.CreatesTenant : OnboardingSignupOutcome.JoinsTenant;
            state.Signup = new(outcome, line.Location);
        }

        RejectBody(context, line);
    }

    static void ParseInvitation(ParserContext context, SourceLine line, State state)
    {
        var tenant = TenantInvitationRegex().Match(line.Content);
        if (tenant.Success)
        {
            AddInvitation(context, state, new(OnboardingInvitationKind.Tenant, [], SplitNames(tenant.Groups[1].Value), line.Location));
            RejectBody(context, line);
            return;
        }

        if (line.Content != "invite member")
        {
            context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Invalid invitation '{line.Content}' - expected 'invite tenant as <Persona>' or 'invite member'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        IReadOnlyList<string>? by = null;
        IReadOnlyList<string>? grant = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = MemberDirectiveRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Invalid member invitation line '{child.Content}' - expected 'by <Persona>, ...' or 'grant <Persona>, ...'", child.Location);
            }
            else if (match.Groups[1].Value == "by" ? by is not null : grant is not null)
            {
                Duplicate(context, match.Groups[1].Value, child);
            }
            else if (match.Groups[1].Value == "by")
            {
                by = SplitNames(match.Groups[2].Value);
            }
            else
            {
                grant = SplitNames(match.Groups[2].Value);
            }

            RejectBody(context, child);
        }

        if (by is null || grant is null)
        {
            context.Error(DiagnosticCodes.InvalidOnboardingDirective, "A member invitation says who may send it and what it grants - declare both 'by <Persona>, ...' and 'grant <Persona>, ...'", line.Location);
        }

        AddInvitation(context, state, new(OnboardingInvitationKind.Member, by ?? [], grant ?? [], line.Location));
    }

    static void AddInvitation(ParserContext context, State state, OnboardingInvitationSyntax invitation)
    {
        if (state.Invitations.Exists(existing => existing.Kind == invitation.Kind))
        {
            context.Error(DiagnosticCodes.DuplicateOnboardingDirective, $"Duplicate invitation of a {invitation.Kind.ToString().ToLowerInvariant()} - an onboarding block has at most one of each kind", invitation.Location);
            return;
        }

        state.Invitations.Add(invitation);
    }

    static void ParseLegalTerms(ParserContext context, SourceLine line, State state)
    {
        if (line.Content != "legal terms")
        {
            context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Invalid legal declaration '{line.Content}' - expected 'legal terms'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        var documents = new List<FileReferenceSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (FileReferenceParser.IsDirective(child))
            {
                documents.Add(FileReferenceParser.Parse(context, child));
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Invalid legal terms line '{child.Content}' - expected 'file <path>'", child.Location);
            }

            RejectBody(context, child);
        }

        if (documents.Count == 0)
        {
            context.Error(DiagnosticCodes.InvalidOnboardingDirective, "Empty 'legal terms' - name at least one document with 'file <path>'", line.Location);
        }

        if (state.LegalTerms is not null)
        {
            Duplicate(context, "legal terms", line);
        }
        else
        {
            state.LegalTerms = new(documents, line.Location);
        }
    }

    static void ParseOffering(ParserContext context, SourceLine line, State state)
    {
        var match = OfferingRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Invalid offering '{line.Content}' - expected 'offering <Name>'", line.Location);
        }
        else if (state.Offering is not null)
        {
            Duplicate(context, "offering", line);
        }
        else
        {
            state.Offering = new(match.Groups[1].Value, line.Location);
        }

        RejectBody(context, line);
    }

    static void ParseEmails(ParserContext context, SourceLine line, State state)
    {
        if (line.Content != "emails")
        {
            context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Invalid emails declaration '{line.Content}' - expected 'emails'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        var templates = new List<OnboardingEmailSyntax>();
        OnboardingImagesSyntax? images = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var email = EmailRegex().Match(child.Content);
            var folder = ImagesRegex().Match(child.Content);
            if (folder.Success)
            {
                var path = folder.Groups[1].Value.Trim();
                if (images is not null)
                {
                    Duplicate(context, "images", child);
                }
                else
                {
                    FileReferenceParser.WarnWhenAbsolute(context, path, child.Location);
                    images = new(path, child.Location);
                }
            }
            else if (email.Success)
            {
                ParseTemplate(context, child, email, templates);
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Invalid emails line '{child.Content}' - expected '<kind> file <path>' or 'images <folder>'", child.Location);
            }

            RejectBody(context, child);
        }

        if (state.Emails is not null)
        {
            Duplicate(context, "emails", line);
        }
        else
        {
            state.Emails = new(templates, images, line.Location);
        }
    }

    static void ParseTemplate(ParserContext context, SourceLine line, Match email, List<OnboardingEmailSyntax> templates)
    {
        var kind = email.Groups[1].Value;
        if (!OnboardingEmailSyntax.Kinds.Contains(kind))
        {
            context.Error(DiagnosticCodes.UnknownOnboardingEmail, $"Unknown email '{kind}' - onboarding sends {string.Join(", ", OnboardingEmailSyntax.Kinds)}", line.Location);
        }
        else if (templates.Exists(template => template.Kind == kind))
        {
            Duplicate(context, $"{kind} email", line);
        }
        else
        {
            var path = email.Groups[2].Value.Trim();
            var fileLocation = line.LocationAt(email.Groups[2].Index);
            FileReferenceParser.WarnWhenAbsolute(context, path, fileLocation);
            templates.Add(new(kind, new(path, fileLocation), line.Location));
        }
    }

    static IReadOnlyList<string> SplitNames(string list) =>
        [.. list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    static void Duplicate(ParserContext context, string what, SourceLine line) =>
        context.Error(DiagnosticCodes.DuplicateOnboardingDirective, $"Duplicate '{what}' - an onboarding block states each of these at most once", line.Location);

    static void RejectBody(ParserContext context, SourceLine line)
    {
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            context.Error(DiagnosticCodes.InvalidOnboardingDirective, $"Unexpected '{child.Content}' under '{line.Content}'", child.Location);
            context.SkipBlock(child.Indent);
        }
    }

    [GeneratedRegex(@"^signup\s+open\s+(creates|joins)\s+tenant$", RegexOptions.NonBacktracking)]
    private static partial Regex SignupRegex();

    [GeneratedRegex(@"^invite\s+tenant\s+as\s+(" + Personas + ")$", RegexOptions.NonBacktracking)]
    private static partial Regex TenantInvitationRegex();

    [GeneratedRegex(@"^(by|grant)\s+(" + Personas + ")$", RegexOptions.NonBacktracking)]
    private static partial Regex MemberDirectiveRegex();

    [GeneratedRegex(@"^offering\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex OfferingRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s+file\s+(\S.*)$", RegexOptions.NonBacktracking)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^images\s+(\S.*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ImagesRegex();

    sealed class State
    {
        public OnboardingSignupSyntax? Signup { get; set; }

        public List<OnboardingInvitationSyntax> Invitations { get; } = [];

        public OnboardingLegalTermsSyntax? LegalTerms { get; set; }

        public OnboardingOfferingSyntax? Offering { get; set; }

        public OnboardingEmailsSyntax? Emails { get; set; }
    }
}
