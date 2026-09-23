// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the outcome of resolving an <c>$eventContext.&lt;path&gt;</c> against the <see cref="EventContextCatalog"/>.
/// </summary>
/// <param name="Path">The path that was resolved, without the <c>$eventContext.</c> prefix.</param>
/// <param name="Status">The <see cref="EventContextPathStatus"/> of the resolution.</param>
public record EventContextPathResolution(string Path, EventContextPathStatus Status)
{
    /// <summary>
    /// Gets the last member resolved - the member the path names when <see cref="Status"/> is
    /// <see cref="EventContextPathStatus.Known"/>, otherwise the member the offending segment sits below, if any.
    /// </summary>
    public EventContextMember? Member { get; init; }

    /// <summary>
    /// Gets the member each segment names, in path order, when <see cref="Status"/> is <see cref="EventContextPathStatus.Known"/>;
    /// otherwise empty.
    /// </summary>
    /// <remarks>
    /// The names are the catalog's own, so joining them gives the canonical camelCase spelling of a path written with a
    /// first letter uppercased or a function written with parentheses.
    /// </remarks>
    public IReadOnlyList<EventContextMember> Members { get; init; } = [];

    /// <summary>
    /// Gets the segment that did not resolve, or <c>null</c> when the path is <see cref="EventContextPathStatus.Known"/>.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// Gets the members that could have been named where the path stopped resolving.
    /// </summary>
    public IReadOnlyList<EventContextMember> Expected { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the path names a member the catalog knows.
    /// </summary>
    public bool IsKnown => Status == EventContextPathStatus.Known;
}
