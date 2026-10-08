// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents one labeled action selecting a command from ordered conditions on the subject item.
/// </summary>
/// <param name="Label">The localizable display label.</param>
/// <param name="Alternatives">The alternatives in authored order.</param>
/// <param name="Location">The source location of the action.</param>
public record ScreenGuardedActionSyntax(string Label, IEnumerable<ScreenActionAlternativeSyntax> Alternatives, SourceLocation Location) : ScreenDirectiveSyntax(Location)
{
    /// <summary>
    /// Gets the explicit fallback, or null to hide the action when no alternative matches.
    /// </summary>
    public ScreenActionOtherwiseSyntax? Otherwise { get; init; }

    /// <summary>
    /// Gets the navigation performed after successful execution of the selected command.
    /// </summary>
    public ScreenNavigateSyntax? Navigate { get; init; }
}

/// <summary>
/// Represents a guarded command choice.
/// </summary>
/// <param name="Condition">The structured condition on the subject item.</param>
/// <param name="Command">The command to execute when this is the first matching alternative.</param>
/// <param name="Location">The source location of the alternative.</param>
public record ScreenActionAlternativeSyntax(ConditionSyntax Condition, string Command, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the explicit input bindings for this command.
    /// </summary>
    public IEnumerable<InteractionArgumentSyntax> Arguments { get; init; } = [];
}

/// <summary>
/// Represents the explicit outcome when no guarded alternative matches.
/// </summary>
/// <param name="Outcome">Whether the action hides or executes a fallback command.</param>
/// <param name="Command">The fallback command, absent for a hidden outcome.</param>
/// <param name="Location">The source location of the fallback.</param>
public record ScreenActionOtherwiseSyntax(ScreenActionOtherwiseOutcome Outcome, string? Command, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the explicit input bindings for the fallback command.
    /// </summary>
    public IEnumerable<InteractionArgumentSyntax> Arguments { get; init; } = [];
}
