// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the base of a <c>constraint</c> declaration - an append time invariant.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ConstraintSyntax(string Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Additional unique rules sharing this constraint's name and claim.
    /// </summary>
    /// <remarks>
    /// Chronicle merges same-named unique event types as mutually exclusive alternatives
    /// (<c>Clients/DotNET/Events/Constraints/ConstraintBuilder.cs:113-145</c>) and supports multiple
    /// property targets through repeated <c>On</c> calls
    /// (<c>Clients/DotNET/Events/Constraints/IUniqueConstraintBuilder.cs:26-37</c>).
    /// </remarks>
    public IEnumerable<ConstraintSyntax> AdditionalRules { get; init; } = [];

    /// <summary>
    /// Events that release a claim, in declaration order.
    /// </summary>
    /// <remarks>
    /// Chronicle permits repeated <c>RemovedWith</c> declarations for both constraint kinds
    /// (<c>Clients/DotNET/Events/Constraints/IConstraintBuilder.cs:61-75</c>,
    /// <c>IUniqueConstraintBuilder.cs:45-65</c>).
    /// </remarks>
    public IEnumerable<string> ReleasedBy { get; init; } = [];

    /// <summary>
    /// Whether property values are compared without regard to casing.
    /// </summary>
    public bool IgnoreCasing { get; init; }

    /// <summary>
    /// The declared violation message, or null for the default message.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Represents a <c>unique &lt;property&gt; on &lt;event&gt;</c> constraint.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="Property">The property that must be unique.</param>
/// <param name="Event">The event the property lives on.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record UniquePropertyConstraintSyntax(string Name, string Property, string Event, SourceLocation Location) : ConstraintSyntax(Name, Location)
{
    /// <summary>
    /// Further properties of the composite key, in declaration order.
    /// </summary>
    /// <remarks>
    /// Chronicle accepts multiple properties in <c>On</c> and hashes their joined values
    /// (<c>Clients/DotNET/Events/Constraints/IUniqueConstraintBuilder.cs:26-37</c>,
    /// <c>Kernel/Core/Events/Constraints/UniqueConstraintDefinitionExtensions.cs:53-61</c>).
    /// </remarks>
    public IEnumerable<string> AdditionalProperties { get; init; } = [];
}

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
