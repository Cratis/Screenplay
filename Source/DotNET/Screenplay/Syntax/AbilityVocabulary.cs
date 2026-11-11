// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The effect classes an ability can declare; the class says what a retry may do when an attempt's outcome is unknown.
/// </summary>
public static class AbilityEffectClasses
{
    /// <summary>
    /// The class of an ability with no external effect, such as a lookup; it can be retried freely.
    /// </summary>
    public const string Safe = "safe";

    /// <summary>
    /// The class of an ability that has its effect once when repeated with the same idempotency key.
    /// </summary>
    public const string Idempotent = "idempotent";

    /// <summary>
    /// The class of an ability that may repeat its effect when repeated; it is never retried and stops for reconciliation. This is the default.
    /// </summary>
    public const string Once = "once";

    /// <summary>
    /// Gets every effect class.
    /// </summary>
    public static ImmutableArray<string> All { get; } = [Safe, Idempotent, Once];

    /// <summary>
    /// Resolves the effective class of an ability, applying the <see cref="Once"/> default.
    /// </summary>
    /// <param name="written">The class as written, or null when omitted.</param>
    /// <returns>The written class, or <see cref="Once"/>.</returns>
    public static string Effective(string? written) => written ?? Once;
}

/// <summary>
/// The outcome names an ability cannot declare because they are outlets that invocation sites already use.
/// </summary>
public static class AbilityOutcomes
{
    /// <summary>
    /// Gets the reserved outlet names, compared ignoring case.
    /// </summary>
    public static ImmutableHashSet<string> ReservedOutlets { get; } =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "next", "refused", "failed", "returned", "expired");
}
