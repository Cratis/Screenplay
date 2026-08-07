// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Languages;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayLanguageRegistry"/> holding the languages the
/// compiler knows about, plus whatever a consumer adds to them.
/// </summary>
/// <param name="inlineLanguages">Languages to recognize in addition to <see cref="BuiltInInlineLanguages"/>.</param>
public sealed class ScreenplayLanguageRegistry(IEnumerable<string>? inlineLanguages = null) : IScreenplayLanguageRegistry
{
    /// <summary>
    /// The languages an inline code block may be written in without anything being registered.
    /// </summary>
    /// <remarks>
    /// These are the ones the surrounding tooling already understands end to end - a Stage renders them and
    /// an editor highlights them. A registered language gets neither for free, which is the honest difference
    /// between a language the compiler ships with and one it merely carries.
    /// </remarks>
    public static readonly IReadOnlySet<string> BuiltInInlineLanguages =
        new HashSet<string>(StringComparer.Ordinal) { "csharp", "typescript", "react", "html", "sql" };

    /// <summary>
    /// Gets the registry a compiler uses when it is not given one.
    /// </summary>
    public static IScreenplayLanguageRegistry Default { get; } = new ScreenplayLanguageRegistry();

    /// <inheritdoc/>
    public IReadOnlySet<string> InlineLanguages { get; } =
        new HashSet<string>(BuiltInInlineLanguages.Concat(inlineLanguages ?? []), StringComparer.Ordinal);
}
