// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Design;

/// <summary>
/// The flat result of resolving a theme of a design system in one mode: every token's value and every component part's value.
/// </summary>
/// <param name="DesignSystem">The name of the design system.</param>
/// <param name="Theme">The name of the theme.</param>
/// <param name="Mode">The mode, or <see langword="null"/> when the design system declares none.</param>
/// <param name="Tokens">The resolved value of each token that has one, keyed by <c>&lt;category&gt;.&lt;name&gt;</c>.</param>
/// <param name="Components">The resolved components.</param>
/// <param name="Problems">What could not be resolved, each as a message naming the token or part.</param>
public sealed record ResolvedDesignTheme(
    string DesignSystem,
    string Theme,
    string? Mode,
    IReadOnlyDictionary<string, string> Tokens,
    IReadOnlyList<ResolvedDesignComponent> Components,
    IReadOnlyList<string> Problems);

/// <summary>
/// A component of a design system with its parts resolved.
/// </summary>
/// <param name="Name">The component's name.</param>
/// <param name="Variants">The resolved variants.</param>
public sealed record ResolvedDesignComponent(string Name, IReadOnlyList<ResolvedDesignVariant> Variants);

/// <summary>
/// A variant of a component with its parts, and the parts of each of its states, resolved.
/// </summary>
/// <param name="Name">The variant's name.</param>
/// <param name="Parts">The resolved value of each part of the variant.</param>
/// <param name="States">Per state, the resolved value of each part the state overrides merged over the variant's parts.</param>
public sealed record ResolvedDesignVariant(
    string Name,
    IReadOnlyDictionary<string, string> Parts,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> States);
