// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Languages;

/// <summary>
/// Defines what a Screenplay document may be extended with beyond the language's own constructs.
/// </summary>
/// <remarks>
/// The editor has had a registry for a while; the compiler that Studio and Stage actually consume did not, so
/// a third party sub-language highlighted correctly and was then silently discarded. Extending the language
/// meant editing the compiler, which is the opposite of what the documentation promised.
/// <para>
/// A registry says what the compiler recognizes. It never says what those things <em>mean</em> - a registered
/// inline language is text the compiler carries rather than reads, and whoever registered it is what makes
/// sense of it. That boundary is what lets the set be open at all.
/// </para>
/// </remarks>
public interface IScreenplayLanguageRegistry
{
    /// <summary>
    /// Gets the languages an inline code block may be written in.
    /// </summary>
    IReadOnlySet<string> InlineLanguages { get; }
}
