// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents everything a reducer rule is given when it runs - the state so far, the event being folded in,
/// and the circumstances it arrived under.
/// </summary>
/// <param name="State">The read model as it stands before this event, and <c>null</c> for the first event on
/// an instance - a reducer is what builds the state, so nothing built it before the first fold.</param>
/// <param name="Event">The event being folded in.</param>
/// <param name="Key">The identity of the read-model instance being built.</param>
/// <param name="Tenant">The <see cref="TenantId"/> the events are being reduced for.</param>
/// <param name="Occurred">When the event being folded in occurred.</param>
/// <param name="SequenceNumber">The position of the event in its sequence.</param>
/// <remarks>
/// <para>
/// This is the type a reducer rule's inline <c>csharp</c> block and its <c>file</c> reference compile against
/// - it is in scope as <c>context</c>. A rule answers with the read model as it stands after the event.
/// </para>
/// <para>
/// <see cref="State"/> is nullable and the rest is not, which is the whole shape of a reduction: every fold
/// after the first is given what the previous one returned, and the first is given nothing to build on. A rule
/// that does not handle the null case is a rule that only works on an instance that already exists.
/// </para>
/// </remarks>
public record ReducerContext(
    dynamic? State,
    dynamic Event,
    string Key,
    TenantId Tenant,
    DateTimeOffset Occurred,
    long SequenceNumber)
{
    /// <summary>
    /// Gets a value indicating whether this is the first event folded into the instance, and so whether
    /// <see cref="State"/> has to be built rather than changed.
    /// </summary>
    public bool IsFirst => State is null;
}
