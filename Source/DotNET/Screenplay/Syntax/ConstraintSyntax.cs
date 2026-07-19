// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the base of a <c>constraint</c> declaration - an append time invariant.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ConstraintSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>unique &lt;property&gt; on &lt;event&gt;</c> constraint.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="Property">The property that must be unique.</param>
/// <param name="Event">The event the property lives on.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record UniquePropertyConstraintSyntax(string Name, string Property, string Event, SourceLocation Location) : ConstraintSyntax(Name, Location);

/// <summary>
/// Represents a <c>unique event &lt;event&gt;</c> constraint - only one such event per event source.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="Event">The event that must be unique.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record UniqueEventConstraintSyntax(string Name, string Event, SourceLocation Location) : ConstraintSyntax(Name, Location);

/// <summary>
/// Represents a constraint implemented in an external code file.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> holding the implementation.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record FileConstraintSyntax(string Name, FileReferenceSyntax File, SourceLocation Location) : ConstraintSyntax(Name, Location);
