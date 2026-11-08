// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Design;

/// <summary>
/// The closed vocabularies of a design system: token categories, component parts and states, and which token categories each part accepts.
/// </summary>
public static class DesignVocabulary
{
    static readonly Dictionary<string, string[]> _partCategories = new(StringComparer.Ordinal)
    {
        ["text"] = ["color"],
        ["background"] = ["color"],
        ["border"] = ["color", "border"],
        ["radius"] = ["radius"],
        ["height"] = ["size", "space"],
        ["padding"] = ["space", "size"],
        ["shadow"] = ["shadow"],
        ["font"] = ["font", "type"]
    };

    /// <summary>
    /// Gets the token categories.
    /// </summary>
    public static IReadOnlyList<string> Categories { get; } = ["color", "space", "size", "radius", "border", "shadow", "font", "type", "motion", "opacity", "z"];

    /// <summary>
    /// Gets the component parts.
    /// </summary>
    public static IReadOnlyList<string> Parts { get; } = ["text", "background", "border", "radius", "height", "padding", "shadow", "font"];

    /// <summary>
    /// Gets the component states.
    /// </summary>
    public static IReadOnlyList<string> States { get; } = ["default", "hover", "focus", "active", "disabled", "selected"];

    /// <summary>
    /// Gets the token categories a component part accepts.
    /// </summary>
    /// <param name="part">The part word.</param>
    /// <returns>The accepted categories; empty for an unknown part.</returns>
    public static IReadOnlyList<string> CategoriesFor(string part) => _partCategories.TryGetValue(part, out var categories) ? categories : [];
}
