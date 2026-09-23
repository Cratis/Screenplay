// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Holds the event-context paths a projection may read, with the portable primitive each one carries.
/// </summary>
/// <remarks>
/// <para>
/// Chronicle resolves <c>$eventContext.&lt;path&gt;</c> by reflection over its <c>EventContext</c> record
/// (<c>Chronicle/Source/Kernel/Concepts/Events/EventContext.cs:28-44</c>) and throws for a path the record does not have, so the
/// only early error an author can get is this one. The list is deliberately small: scalar members with a portable meaning.
/// Collections (<c>causation</c>, <c>tags</c>), the replay-dependent <c>observationState</c> and the non-existent <c>causationId</c>
/// are left out. <c>eventSourceId</c> is not listed because it binds to the event source identity value instead.
/// </para>
/// <para>
/// This is an internal stopgap: issue #217 replaces it with the shared event-context catalog every surface generates from.
/// </para>
/// </remarks>
static class SemanticEventContextPaths
{
    /// <summary>
    /// The event-context path that reads the event source identity.
    /// </summary>
    internal const string EventSourceId = "eventSourceId";

    static readonly FrozenDictionary<string, SemanticPrimitiveType> _paths = new Dictionary<string, SemanticPrimitiveType>(StringComparer.Ordinal)
    {
        ["occurred"] = SemanticPrimitiveType.DateTime,
        ["sequenceNumber"] = SemanticPrimitiveType.WholeNumber,
        ["correlationId"] = SemanticPrimitiveType.Uuid,
        ["eventType.id"] = SemanticPrimitiveType.Text,
        ["eventType.generation"] = SemanticPrimitiveType.WholeNumber,
        ["eventSourceType"] = SemanticPrimitiveType.Text,
        ["eventStreamType"] = SemanticPrimitiveType.Text,
        ["eventStreamId"] = SemanticPrimitiveType.Text,
        ["eventStore"] = SemanticPrimitiveType.Text,
        ["namespace"] = SemanticPrimitiveType.Text,
        ["subject"] = SemanticPrimitiveType.Text,
        ["causedBy.subject"] = SemanticPrimitiveType.Text,
        ["causedBy.name"] = SemanticPrimitiveType.Text,
        ["causedBy.userName"] = SemanticPrimitiveType.Text
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Gets every admitted path in ordinal order.
    /// </summary>
    internal static IEnumerable<string> All => _paths.Keys.Order(StringComparer.Ordinal);

    /// <summary>
    /// Gets the portable primitive an admitted path carries.
    /// </summary>
    /// <param name="path">The dotted camel-case event-context path.</param>
    /// <param name="primitive">The primitive when admitted.</param>
    /// <returns>Whether the path is admitted.</returns>
    internal static bool TryGetPrimitive(string path, out SemanticPrimitiveType primitive) => _paths.TryGetValue(path, out primitive);
}
