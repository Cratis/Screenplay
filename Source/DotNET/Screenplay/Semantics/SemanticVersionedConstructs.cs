// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Tells which constructs a bound model holds that a later ESM version admitted, so a host that carries some of a version's
/// constructs and not others can judge a model by what it holds instead of by the number it binds as.
/// </summary>
public static class SemanticVersionedConstructs
{
    /// <summary>
    /// Gets whether a command compares a Date, DateTime or Instant value (ESM v26).
    /// </summary>
    /// <param name="application">The bound application.</param>
    /// <returns><see langword="true"/> when a command compares chronologically.</returns>
    public static bool UsesDateComparisons(SemanticApplication application) => SemanticModelValidator.UsesDateComparisons(application);

    /// <summary>
    /// Gets whether a read model is keyed by several identifier parts (ESM v27).
    /// </summary>
    /// <param name="application">The bound application.</param>
    /// <returns><see langword="true"/> when a read model has a composite key.</returns>
    public static bool UsesCompositeReadModelKeys(SemanticApplication application) => SemanticModelValidator.UsesCompositeReadModelKeys(application);

    /// <summary>
    /// Gets whether a command compares Uuid values (ESM v28).
    /// </summary>
    /// <param name="application">The bound application.</param>
    /// <returns><see langword="true"/> when a command compares Uuid values.</returns>
    public static bool UsesUuidComparisons(SemanticApplication application) => SemanticModelValidator.UsesUuidComparisons(application);
}
