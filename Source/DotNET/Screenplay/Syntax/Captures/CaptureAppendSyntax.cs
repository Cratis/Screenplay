// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Captures;

/// <summary>
/// Defines the kinds of capture triggers.
/// </summary>
public enum CaptureWhenKind
{
    /// <summary>
    /// Append when a named property changes.
    /// </summary>
    PropertyChanged = 0,

    /// <summary>
    /// Append when an item appears in the source.
    /// </summary>
    Added = 1,

    /// <summary>
    /// Append when an item disappears from the source.
    /// </summary>
    Removed = 2,

    /// <summary>
    /// Append when an item changes in the source.
    /// </summary>
    /// <remarks>
    /// This value predates the reconciliation of Screenplay's Capture syntax with Chronicle's Capture
    /// Declaration Language (CDL) grammar and has no corresponding construct in that grammar's
    /// <c>WhenClauseType</c> as of this migration - Chronicle only distinguishes <c>added</c>/<c>removed</c>
    /// (collection membership) from a named property change. Left in place rather than silently removed;
    /// a future reader/reviewer should decide whether to deprecate it once CDL parity is the sole authority.
    /// </remarks>
    Changed = 3,

    /// <summary>
    /// Append when any of several named properties changes, combined with <c>or</c>.
    /// </summary>
    LogicalOr = 4,

    /// <summary>
    /// Append when all of several named properties change, combined with <c>and</c>.
    /// </summary>
    LogicalAnd = 5,

    /// <summary>
    /// Append when a named property transitions from one specific value to another.
    /// </summary>
    ValueTransition = 6,

    /// <summary>
    /// Append when a raw template-literal expression evaluates to true.
    /// </summary>
    Expression = 7
}

/// <summary>
/// Represents an <c>append</c> declaration - the event to append when the source changes.
/// </summary>
/// <param name="Event">The name of the event to append.</param>
/// <param name="When">The <see cref="CaptureWhenSyntax"/> describing when to append.</param>
/// <param name="Mappings">The <see cref="PropertyMappingSyntax">property mappings</see> for the appended event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Tags">The <see cref="TagSyntax">tags</see> applied to the appended event.</param>
public record CaptureAppendSyntax(
    string Event,
    CaptureWhenSyntax? When,
    IEnumerable<PropertyMappingSyntax> Mappings,
    SourceLocation Location,
    IEnumerable<TagSyntax>? Tags = null) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>when</c> trigger of an append.
/// </summary>
/// <param name="Kind">The <see cref="CaptureWhenKind"/> of the trigger.</param>
/// <param name="Properties">
/// The properties participating in the trigger - a single property for <see cref="CaptureWhenKind.PropertyChanged"/>
/// and <see cref="CaptureWhenKind.ValueTransition"/>, or several for <see cref="CaptureWhenKind.LogicalOr"/> and
/// <see cref="CaptureWhenKind.LogicalAnd"/>.
/// </param>
/// <param name="FromValue">The source value of a <see cref="CaptureWhenKind.ValueTransition"/>.</param>
/// <param name="ToValue">The target value of a <see cref="CaptureWhenKind.ValueTransition"/>.</param>
/// <param name="Expression">The raw, unparsed template-literal expression of a <see cref="CaptureWhenKind.Expression"/> trigger.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureWhenSyntax(
    CaptureWhenKind Kind,
    IEnumerable<string> Properties,
    string? FromValue,
    string? ToValue,
    string? Expression,
    SourceLocation Location) : SyntaxNode(Location);
