// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Represents the result of admitting a model's screens to its executable semantic model.
/// </summary>
/// <param name="Compilation">The compilation holding the admitted screens, or <see langword="null"/> when admission was refused.</param>
/// <param name="Refusals">Why admission was refused; empty when it succeeded.</param>
public sealed record ExecutableScreensResult(SemanticCompilation? Compilation, ImmutableArray<string> Refusals)
{
    /// <summary>
    /// Gets whether the screens were admitted.
    /// </summary>
    public bool Success => Compilation is not null;
}
