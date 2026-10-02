// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents where the top level of a <c>.play</c> file belongs - the application itself, a module, or a
/// feature nested to any depth.
/// </summary>
/// <param name="Scope">The module name followed by the names of the nested features, outermost first. Empty for the application.</param>
/// <remarks>
/// A file is placed by the <c>import</c> that brings it in: written inside <c>module Ordering</c> and
/// <c>feature Orders</c>, it places the imported file at <c>Ordering.Orders</c>, so what the file declares at its
/// top level is the body of that feature.
/// </remarks>
public record PlayPlacement(IReadOnlyList<string> Scope)
{
    /// <summary>
    /// Gets the placement of a whole document - its top level is the application's.
    /// </summary>
    public static readonly PlayPlacement Document = new([]);

    /// <summary>
    /// Gets whether the file is a whole document rather than placed in a module or feature.
    /// </summary>
    public bool IsDocument => Scope.Count == 0;

    /// <summary>
    /// Gets the placement as it reads in a diagnostic, such as <c>module 'Ordering'</c> or <c>feature 'Ordering.Orders'</c>.
    /// </summary>
    public string Description => Scope.Count switch
    {
        0 => "the application",
        1 => $"module '{Scope[0]}'",
        _ => $"feature '{string.Join('.', Scope)}'"
    };

    /// <summary>
    /// Creates the placement of something written at a path within a file that is itself placed here.
    /// </summary>
    /// <param name="scopeWithinFile">The module and feature names around the import inside the file, outermost first.</param>
    /// <returns>The combined <see cref="PlayPlacement"/>.</returns>
    public PlayPlacement Within(IEnumerable<string> scopeWithinFile) => new([.. Scope, .. scopeWithinFile]);

    /// <summary>
    /// Gets whether this placement lies inside another one, or is the same.
    /// </summary>
    /// <param name="other">The <see cref="PlayPlacement"/> to compare with.</param>
    /// <returns><c>true</c> when <paramref name="other"/>'s scope is a prefix of this one.</returns>
    public bool IsWithinOrSame(PlayPlacement other) =>
        other.Scope.Count <= Scope.Count && other.Scope.Select((name, index) => string.Equals(name, Scope[index], StringComparison.Ordinal)).All(same => same);

    /// <inheritdoc/>
    public virtual bool Equals(PlayPlacement? other) =>
        other is not null && Scope.SequenceEqual(other.Scope, StringComparer.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode() => string.Join('.', Scope).GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Description;
}
