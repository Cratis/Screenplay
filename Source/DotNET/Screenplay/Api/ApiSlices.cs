// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// Walks the slices of an executable model together with their module and feature path.
/// </summary>
static class ApiSlices
{
    /// <summary>
    /// Lists every slice in model order with the names leading to it, ending with the slice name.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>The path and slice of every slice.</returns>
    internal static IEnumerable<(ImmutableArray<string> Path, SemanticSlice Slice)> All(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(feature => Feature(feature, [module.Name, feature.Name])));

    static IEnumerable<(ImmutableArray<string> Path, SemanticSlice Slice)> Feature(SemanticFeature feature, ImmutableArray<string> path) =>
        feature.Slices.Select(slice => (path.Add(slice.Name), slice))
            .Concat(feature.Features.SelectMany(child => Feature(child, path.Add(child.Name))));
}
