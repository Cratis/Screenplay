// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Represents <c>given &lt;System&gt;.&lt;Ability&gt; returns</c>, <c>answers &lt;Outcome&gt;</c> or <c>fails "reason"</c> -
/// the answer the invoked ability gives while the specification runs, so the branches that read it can be proved.
/// </summary>
/// <param name="Ability">The <c>System.Ability</c> that answers.</param>
/// <param name="Answer">The answer kind: <c>returns</c>, <c>answers</c> or <c>fails</c>.</param>
/// <param name="Outcome">The declared outcome an <c>answers</c> step names, or <c>null</c>.</param>
/// <param name="Reason">The failure reason a <c>fails</c> step states, or <c>null</c>.</param>
/// <param name="Values">The <see cref="PropertyMappingSyntax">returned values</see> a <c>returns</c> step supplies.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationAbilityAnswerSyntax(
    string Ability,
    string Answer,
    string? Outcome,
    string? Reason,
    IEnumerable<PropertyMappingSyntax> Values,
    SourceLocation Location) : SyntaxNode(Location);
