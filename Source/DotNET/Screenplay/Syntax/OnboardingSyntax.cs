// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>onboarding</c> block declaring how people get into the application:
/// signing up, being invited, accepting legal terms and the emails sent along the way.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// Like <c>authentication</c>, the block is realization metadata. It says what the application offers its
/// newcomers and nothing about how the mail leaves the building - the sender and the mail server are
/// environment values, found by name where the application runs. It never enters the executable semantic model.
/// </remarks>
public record OnboardingSyntax(SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the self-signup declaration. Absence means self-signup is off.
    /// </summary>
    public OnboardingSignupSyntax? Signup { get; init; }

    /// <summary>
    /// Gets the <see cref="OnboardingInvitationSyntax">invitations</see> the application supports, at most one per kind.
    /// </summary>
    public IEnumerable<OnboardingInvitationSyntax> Invitations { get; init; } = [];

    /// <summary>
    /// Gets the <see cref="OnboardingLegalTermsSyntax">legal terms</see> a newcomer must accept, when there are any.
    /// </summary>
    public OnboardingLegalTermsSyntax? LegalTerms { get; init; }

    /// <summary>
    /// Gets the <see cref="OnboardingOfferingSyntax">offering</see> a new tenant starts on, when licensing is involved.
    /// </summary>
    public OnboardingOfferingSyntax? Offering { get; init; }

    /// <summary>
    /// Gets the <see cref="OnboardingEmailsSyntax">emails</see> sent during onboarding.
    /// </summary>
    public OnboardingEmailsSyntax? Emails { get; init; }
}

/// <summary>
/// What a self-signup does.
/// </summary>
public enum OnboardingSignupOutcome
{
    /// <summary>A sign-up creates a new tenant (organization) for the person signing up.</summary>
    CreatesTenant,

    /// <summary>A sign-up asks to join a tenant that already exists.</summary>
    JoinsTenant
}

/// <summary>
/// Represents <c>signup open creates tenant</c> or <c>signup open joins tenant</c>.
/// </summary>
/// <param name="Outcome">What a sign-up leads to.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OnboardingSignupSyntax(OnboardingSignupOutcome Outcome, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// What an invitation is for.
/// </summary>
public enum OnboardingInvitationKind
{
    /// <summary>The invitation creates a new tenant for the invitee.</summary>
    Tenant,

    /// <summary>The invitation brings the invitee into an existing tenant.</summary>
    Member
}

/// <summary>
/// Represents an <c>invite tenant as &lt;Persona&gt;</c> or an <c>invite member</c> block with <c>by</c> and <c>grant</c>.
/// </summary>
/// <param name="Kind">What the invitation is for.</param>
/// <param name="By">The personas allowed to send the invitation; empty for a tenant invitation, which the application's operator sends.</param>
/// <param name="Grant">The personas the invitee is given - the <c>as</c> persona of a tenant invitation, or the <c>grant</c> list of a member invitation.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OnboardingInvitationSyntax(OnboardingInvitationKind Kind, IEnumerable<string> By, IEnumerable<string> Grant, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>legal terms</c> block - the documents a newcomer has to accept.
/// </summary>
/// <param name="Documents">The <see cref="FileReferenceSyntax">files</see>, relative to the repository root, holding the terms.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OnboardingLegalTermsSyntax(IEnumerable<FileReferenceSyntax> Documents, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents <c>offering &lt;Name&gt;</c> - the offering a new tenant starts on.
/// </summary>
/// <param name="Name">The name of the offering. Opaque to the model: it is resolved where the application is provisioned.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OnboardingOfferingSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>emails</c> block - the template per email and the folder holding their images.
/// </summary>
/// <param name="Templates">The <see cref="OnboardingEmailSyntax">templates</see>, at most one per kind.</param>
/// <param name="Images">The folder the templates take their images from, when they have any.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OnboardingEmailsSyntax(IEnumerable<OnboardingEmailSyntax> Templates, OnboardingImagesSyntax? Images, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one <c>&lt;kind&gt; file &lt;path&gt;</c> line of the <c>emails</c> block.
/// </summary>
/// <param name="Kind">Which email it is - <c>invitation</c>, <c>welcome</c> or <c>signupDeclined</c>.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> holding the template.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OnboardingEmailSyntax(string Kind, FileReferenceSyntax File, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// The kinds of email onboarding sends.
    /// </summary>
    public static readonly IReadOnlyList<string> Kinds = ["invitation", "welcome", "signupDeclined"];
}

/// <summary>
/// Represents <c>images &lt;folder&gt;</c> in the <c>emails</c> block.
/// </summary>
/// <param name="Path">The folder, relative to the repository root, holding the images the templates use.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OnboardingImagesSyntax(string Path, SourceLocation Location) : SyntaxNode(Location);
