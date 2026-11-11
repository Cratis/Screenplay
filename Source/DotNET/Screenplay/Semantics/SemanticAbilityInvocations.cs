// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// One input handed to an ability: the input name and the value supplied for it (ESM v51, #1252).
/// </summary>
/// <param name="Input">The name of the ability input.</param>
/// <param name="Source">The value supplied for the input.</param>
public sealed record SemanticAbilityInputMapping(string Input, SemanticExpression Source);

/// <summary>
/// One ordered branch that handles what an ability invocation answered (ESM v51, #1252).
/// </summary>
/// <param name="Selector"><c>returned</c>, <c>failed</c> or the name of a declared outcome.</param>
/// <param name="Produces">The events recorded when the branch is taken.</param>
public sealed record SemanticInvocationBranch(string Selector, ImmutableArray<SemanticProducedEvent> Produces);

/// <summary>
/// A value an ability invocation answered with, read inside the branch that handles it (ESM v51, #1252).
/// </summary>
/// <param name="Source"><c>result</c>, <c>outcome</c> or <c>failure</c>.</param>
/// <param name="Member">The returned field, or the failure member; empty for the outcome name.</param>
/// <param name="Type">The type of the value.</param>
public sealed record SemanticInvocationValueExpression(string Source, string Member, SemanticTypeReference Type)
    : SemanticExpression(SemanticExpressionKind.InvocationValue);

/// <summary>
/// Decides which invocation values a branch can read.
/// </summary>
internal static class InvocationBranchScope
{
    /// <summary>Whether a branch handles a declared outcome rather than success or failure.</summary>
    /// <param name="branch">The branch selector.</param>
    /// <returns><c>true</c> when the selector names an outcome.</returns>
    internal static bool IsOutcome(string? branch) => branch is not null && !Is(branch, "returned") && !Is(branch, "failed");

    /// <summary>Whether a branch selector is the given word.</summary>
    /// <param name="branch">The branch selector.</param>
    /// <param name="word">The word.</param>
    /// <returns><c>true</c> when they are the same.</returns>
    internal static bool Is(string? branch, string word) => string.Equals(branch, word, StringComparison.Ordinal);

    /// <summary>Whether a member can be read from a failure.</summary>
    /// <param name="member">The member.</param>
    /// <returns><c>true</c> for <c>reason</c> and <c>message</c>.</returns>
    internal static bool IsFailureMember(string member) => Is(member, "reason") || Is(member, "message");
}
