// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Represents the base of every mapping line in a projection body.
/// </summary>
/// <param name="Property">The dotted target property path on the read model.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record MappingSyntax(string Property, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an assignment mapping, such as <c>status = "draft"</c>.
/// </summary>
/// <param name="Property">The dotted target property path on the read model.</param>
/// <param name="Source">The <see cref="ExpressionSyntax"/> providing the value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SetMappingSyntax(string Property, ExpressionSyntax Source, SourceLocation Location) : MappingSyntax(Property, Location);

/// <summary>
/// Represents a <c>clear</c> mapping - removes the value the property holds when the event occurs.
/// </summary>
/// <param name="Property">The dotted target property path on the read model.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// Clearing is its own act, not an assignment of nothing, so it is its own node rather than a
/// <see cref="SetMappingSyntax"/> holding a null literal. The older spelling <c>property = null</c> still
/// parses, and still parses to a <see cref="SetMappingSyntax"/> - the two forms stay distinguishable, so a
/// consumer can render each one back the way it was written.
/// </remarks>
public record ClearMappingSyntax(string Property, SourceLocation Location) : MappingSyntax(Property, Location);

/// <summary>
/// Represents an <c>increment</c> mapping - adds one to the property when the event occurs.
/// </summary>
/// <param name="Property">The dotted target property path on the read model.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record IncrementMappingSyntax(string Property, SourceLocation Location) : MappingSyntax(Property, Location);

/// <summary>
/// Represents a <c>decrement</c> mapping - subtracts one from the property when the event occurs.
/// </summary>
/// <param name="Property">The dotted target property path on the read model.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DecrementMappingSyntax(string Property, SourceLocation Location) : MappingSyntax(Property, Location);

/// <summary>
/// Represents a <c>count</c> mapping - counts occurrences of events into the property.
/// </summary>
/// <param name="Property">The dotted target property path on the read model.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CountMappingSyntax(string Property, SourceLocation Location) : MappingSyntax(Property, Location);

/// <summary>
/// Represents an <c>add &lt;property&gt; by &lt;expression&gt;</c> mapping.
/// </summary>
/// <param name="Property">The dotted target property path on the read model.</param>
/// <param name="Value">The <see cref="ExpressionSyntax"/> to add.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AddMappingSyntax(string Property, ExpressionSyntax Value, SourceLocation Location) : MappingSyntax(Property, Location);

/// <summary>
/// Represents a <c>subtract &lt;property&gt; by &lt;expression&gt;</c> mapping.
/// </summary>
/// <param name="Property">The dotted target property path on the read model.</param>
/// <param name="Value">The <see cref="ExpressionSyntax"/> to subtract.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SubtractMappingSyntax(string Property, ExpressionSyntax Value, SourceLocation Location) : MappingSyntax(Property, Location);
