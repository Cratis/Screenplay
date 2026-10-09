// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents one ordered condition selecting an interaction action list.
/// </summary>
/// <param name="Condition">The strict item condition.</param>
/// <param name="Actions">The actions run when this is the first matching alternative.</param>
/// <param name="Location">The location of the when clause.</param>
public record InteractionAlternativeSyntax(
    ConditionSyntax Condition,
    IEnumerable<InteractionActionSyntax> Actions,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the fallback action list of a guarded interaction.
/// </summary>
/// <param name="Actions">The actions run when a subject exists but no alternative matches.</param>
/// <param name="Location">The location of the otherwise clause.</param>
public record InteractionOtherwiseSyntax(
    IEnumerable<InteractionActionSyntax> Actions,
    SourceLocation Location) : SyntaxNode(Location);
