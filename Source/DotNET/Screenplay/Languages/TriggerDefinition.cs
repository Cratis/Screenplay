// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Languages;

/// <summary>
/// Represents a trigger a consumer registers with the compiler - a kind of occurrence a reaction may
/// respond to that no document declares.
/// </summary>
/// <param name="Name">The name a reaction responds to with <c>when</c>.</param>
/// <param name="Values">
/// The values an occurrence hands the reaction, or <c>null</c> when the registration does not say.
/// </param>
/// <remarks>
/// A registration says the name exists, and optionally what an occurrence carries. It never says what makes
/// one occur - that belongs to whoever produces it, and keeping it out of the compiler is what lets the set
/// of triggers be open at all.
/// <para>
/// <see cref="Values"/> distinguishes two different registrations that a plain empty list would flatten
/// together. <c>null</c> is "the registration does not state a shape", so a reaction taking a value from the
/// occurrence is left alone; an empty list is "an occurrence carries nothing", so taking anything from it is
/// reported. Saying nothing and saying nothing-is-carried are not the same claim.
/// </para>
/// </remarks>
public record TriggerDefinition(string Name, IReadOnlyList<string>? Values = null)
{
    /// <summary>
    /// Converts a name to a <see cref="TriggerDefinition"/> that states no shape.
    /// </summary>
    /// <param name="name">The name a reaction responds to.</param>
    /// <remarks>
    /// So that registering the common case reads as the list of names it is -
    /// <c>new ScreenplayLanguageRegistry(triggers: ["GitPushed", "PullRequestMerged"])</c> - without a
    /// consumer having to wrap each one to say nothing more than its name.
    /// </remarks>
    public static implicit operator TriggerDefinition(string name) => new(name);

    /// <summary>
    /// Creates a <see cref="TriggerDefinition"/> from a name.
    /// </summary>
    /// <param name="name">The name a reaction responds to.</param>
    /// <returns>The <see cref="TriggerDefinition"/>.</returns>
    /// <remarks>
    /// The named alternative to the implicit conversion, for a consumer that cannot use one.
    /// </remarks>
    public static TriggerDefinition FromName(string name) => new(name);
}
