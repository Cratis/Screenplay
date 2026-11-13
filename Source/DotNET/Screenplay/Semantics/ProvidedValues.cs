// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// The level of the tree a <c>provide</c> was declared at.
/// </summary>
public enum ProvideLevel
{
    /// <summary>Declared at the application root.</summary>
    Application,

    /// <summary>Declared in a module.</summary>
    Module,

    /// <summary>Declared in a feature.</summary>
    Feature,

    /// <summary>Declared in a slice.</summary>
    Slice
}

/// <summary>
/// Represents the winning declaration of a provided value at one place in the tree.
/// </summary>
/// <param name="Declaration">The nearest <see cref="ProvideSyntax"/> with the name.</param>
/// <param name="Level">The <see cref="ProvideLevel"/> it was declared at.</param>
public sealed record ProvidedValue(ProvideSyntax Declaration, ProvideLevel Level)
{
    /// <summary>
    /// Gets whether the value comes from code or from the host, which the reference evaluator cannot run:
    /// a gate reading it is reported as unsupported, never as allowed or denied.
    /// </summary>
    public bool IsOpaque => Declaration.Source is CodeProvideSourceSyntax or FileProvideSourceSyntax or HostProvideSourceSyntax;
}

/// <summary>
/// Resolves which <c>provide</c> declaration wins for a name at a module, feature or slice: inheritance is by name
/// down the application, module, feature and slice levels, and the nearest declaration wins.
/// </summary>
public static class ProvidedValues
{
    /// <summary>
    /// Gets the provided values in effect for a slice.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> holding the declarations.</param>
    /// <param name="module">The name of the module.</param>
    /// <param name="features">The names of the enclosing features, outermost first.</param>
    /// <param name="slice">The name of the slice, or <c>null</c> for the feature itself.</param>
    /// <returns>The winning declaration for every name, keyed by name.</returns>
    public static IReadOnlyDictionary<string, ProvidedValue> For(ApplicationSyntax application, string module, IReadOnlyList<string> features, string? slice = null)
    {
        var effective = new Dictionary<string, ProvidedValue>(StringComparer.Ordinal);
        Apply(effective, application.Provides, ProvideLevel.Application);
        var owner = application.Modules.FirstOrDefault(candidate => candidate.Name == module);
        if (owner is null)
        {
            return effective;
        }

        Apply(effective, owner.Provides, ProvideLevel.Module);
        var current = owner.Features;
        FeatureSyntax? feature = null;
        foreach (var name in features)
        {
            feature = current.FirstOrDefault(candidate => candidate.Name == name);
            if (feature is null)
            {
                return effective;
            }

            Apply(effective, feature.Provides, ProvideLevel.Feature);
            current = feature.Features;
        }

        if (slice is not null && feature?.Slices.FirstOrDefault(candidate => candidate.Name == slice) is { } owningSlice)
        {
            Apply(effective, owningSlice.Provides, ProvideLevel.Slice);
        }

        return effective;
    }

    static void Apply(Dictionary<string, ProvidedValue> effective, IEnumerable<ProvideSyntax> provides, ProvideLevel level)
    {
        foreach (var provide in provides)
        {
            effective[provide.Name] = new(provide, level);
        }
    }
}
