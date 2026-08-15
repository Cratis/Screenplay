// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Languages;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayLanguageRegistry"/> holding the languages the
/// compiler knows about, plus whatever a consumer adds to them.
/// </summary>
/// <param name="inlineLanguages">Languages to recognize in addition to <see cref="BuiltInInlineLanguages"/>.</param>
/// <param name="triggers">Triggers to recognize in addition to <see cref="BuiltInTriggers"/>.</param>
public sealed class ScreenplayLanguageRegistry(
    IEnumerable<string>? inlineLanguages = null,
    IEnumerable<TriggerDefinition>? triggers = null) : IScreenplayLanguageRegistry
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
    /// The triggers a reaction may respond to without anything being registered, beyond the events and the
    /// triggers a document declares itself.
    /// </summary>
    /// <remarks>
    /// These are the signals the host raises rather than the domain - there is no event to declare for
    /// "the application started", and every application has one.
    /// <para>
    /// They carry no values, and say so rather than staying silent: a signal that the application started
    /// hands the reaction nothing beyond the fact that it happened.
    /// </para>
    /// </remarks>
    public static readonly IReadOnlyList<TriggerDefinition> BuiltInTriggers =
    [
        new("Startup", []),
        new("Shutdown", [])
    ];

    /// <summary>
    /// Gets the registry a compiler uses when it is not given one.
    /// </summary>
    public static IScreenplayLanguageRegistry Default { get; } = new ScreenplayLanguageRegistry();

    /// <inheritdoc/>
    public IReadOnlySet<string> InlineLanguages { get; } =
        new HashSet<string>(BuiltInInlineLanguages.Concat(inlineLanguages ?? []), StringComparer.Ordinal);

    /// <inheritdoc/>
    /// <remarks>
    /// A registration wins over a built-in of the same name, so a host that raises a richer <c>Startup</c>
    /// can say what it carries rather than being overruled by the empty one the language ships.
    /// </remarks>
    public IReadOnlyDictionary<string, TriggerDefinition> Triggers { get; } =
        BuiltInTriggers.Concat(triggers ?? [])
            .GroupBy(trigger => trigger.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
}
