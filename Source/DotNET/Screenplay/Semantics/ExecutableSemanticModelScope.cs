// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Narrows an executable semantic model to one module, feature or slice.
/// </summary>
/// <remarks>
/// The excerpt keeps every application-wide declaration (concepts, types, policies, triggers, event sources,
/// lifecycles and graphs) so references from the selected declarations still resolve, and drops every module,
/// feature and slice outside the selected path. Its revision identifies the excerpt, not the whole model.
/// </remarks>
public static class ExecutableSemanticModelScope
{
    /// <summary>
    /// Selects the module, feature or slice a dotted scope names.
    /// </summary>
    /// <param name="model">The whole executable model.</param>
    /// <param name="scope">The scope as <c>Module[.Feature...][.Slice]</c>.</param>
    /// <returns>The excerpt holding only the selected path.</returns>
    /// <exception cref="InvalidSemanticModelScope">The scope does not name exactly one module, feature or slice.</exception>
    public static ExecutableSemanticModel Select(ExecutableSemanticModel model, string scope)
    {
        var parts = scope.Split('.');
        if (parts.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidSemanticModelScope($"Scope '{scope}' must be Module[.Feature...][.Slice] with no empty parts.");
        }

        var module = model.Application.Modules.SingleOrDefault(candidate => candidate.Name == parts[0]) ??
            throw new InvalidSemanticModelScope($"Scope '{scope}' does not name a module in the model.");
        var features = parts.Length == 1 ? module.Features : [SelectFeature(module.Features, parts[1..], scope)];
        var application = model.Application with { Modules = [module with { Features = features }] };

        return ExecutableSemanticModel.CreateValidated(model.LanguageVersion, model.SemanticVersion, application);
    }

    static SemanticFeature SelectFeature(ImmutableArray<SemanticFeature> features, string[] parts, string scope)
    {
        var name = parts[0];
        var feature = features.SingleOrDefault(candidate => candidate.Name == name) ??
            throw new InvalidSemanticModelScope($"Scope '{scope}' does not name a feature or slice in the model.");
        if (parts.Length == 1) return feature;
        var next = parts[1];
        if (parts.Length == 2 && feature.Slices.SingleOrDefault(slice => slice.Name == next) is { } slice)
        {
            if (feature.Features.Any(candidate => candidate.Name == next))
            {
                throw new InvalidSemanticModelScope($"Scope '{scope}' is ambiguous: '{next}' names both a feature and a slice.");
            }

            return feature with { Features = [], Slices = [slice] };
        }

        return feature with { Features = [SelectFeature(feature.Features, parts[1..], scope)], Slices = [] };
    }
}
