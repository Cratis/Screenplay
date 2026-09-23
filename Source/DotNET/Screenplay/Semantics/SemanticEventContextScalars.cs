// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Decides which <c>$eventContext.&lt;path&gt;</c> reads ESM v1 admits, reading the shared <see cref="EventContextCatalog"/>.
/// </summary>
/// <remarks>
/// <para>
/// The catalog says what Chronicle can resolve (<c>Chronicle/Source/Kernel/Concepts/Events/EventContext.cs:28-44</c>); this says what
/// of that is one portable scalar value a projection may store. A path is admitted when it names a value whose underlying CLR type
/// maps to a <see cref="SemanticPrimitiveType"/>. The admitted spelling is canonical: camelCase as the catalog writes it, and without
/// the trailing <c>value</c> of a concept, which reads the same value as the member it belongs to.
/// </para>
/// <para>What stays out, and why:</para>
/// <list type="bullet">
/// <item><description>Collections - <c>causation</c> and <c>tags</c> - and anything below them, which has no addressing grammar.</description></item>
/// <item><description>Composites without a leaf - <c>eventType</c>, <c>causedBy</c> - which are not one value.</description></item>
/// <item><description>The derived function <c>occurred.Week</c>. It is a computation, not a member: Chronicle evaluates
/// <c>ISOWeek.GetWeekOfYear</c> on the occurrence time (<c>Chronicle/Source/Infrastructure/Properties/DerivedPropertyFunctions.cs:43-48</c>).
/// ESM has no derived-value shape, so admitting it as a plain path would leave every other realization to rediscover ISO 8601
/// week numbering from a path name.</description></item>
/// <item><description>Paths through <c>causedBy.onBehalfOf</c>. The member is optional, and Chronicle's reflective read instantiates
/// a null intermediate instead of yielding null (<c>Chronicle/Source/Infrastructure/Objects/ObjectExtensions.cs:74-79</c>), so the
/// value is not well defined when nobody acted on behalf of anyone.</description></item>
/// <item><description><c>observationState</c>, whose enum type has no portable primitive - and whose value differs between live
/// processing and a replay of the same event.</description></item>
/// </list>
/// <para><c>eventSourceId</c> is admitted but binds to the event source identity value instead of an event-context read.</para>
/// </remarks>
static class SemanticEventContextScalars
{
    /// <summary>
    /// The canonical event-context path that reads the event source identity.
    /// </summary>
    internal const string EventSourceId = "eventSourceId";

    const string ConceptValue = "value";
    const string OnBehalfOf = "onBehalfOf";

    static readonly FrozenDictionary<string, SemanticPrimitiveType> _primitives = new Dictionary<string, SemanticPrimitiveType>(StringComparer.Ordinal)
    {
        ["string"] = SemanticPrimitiveType.Text,
        ["Guid"] = SemanticPrimitiveType.Uuid,
        ["uint"] = SemanticPrimitiveType.WholeNumber,
        ["ulong"] = SemanticPrimitiveType.WholeNumber,
        ["bool"] = SemanticPrimitiveType.Boolean,
        ["DateTimeOffset"] = SemanticPrimitiveType.DateTime
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Gets every admitted canonical path, event source identity included, in catalog order.
    /// </summary>
    internal static IReadOnlyList<string> All { get; } =
    [
        .. EventContextCatalog.Paths
            .Select(path => Resolve(path.Path))
            .Where(scalar => scalar.IsAdmitted)
            .Select(scalar => scalar.Path)
            .Distinct(StringComparer.Ordinal)
    ];

    /// <summary>
    /// Resolves a path, as written after <c>$eventContext.</c>, to what ESM v1 admits.
    /// </summary>
    /// <param name="path">The path as written.</param>
    /// <returns>The <see cref="SemanticEventContextScalar"/>.</returns>
    internal static SemanticEventContextScalar Resolve(string path)
    {
        var resolution = EventContextCatalog.Resolve(path);
        if (!resolution.IsKnown)
        {
            return SemanticEventContextScalar.Rejected(
                path,
                resolution.Status == EventContextPathStatus.BelowCollection
                    ? $"'{resolution.Member!.Name}' is a collection and cannot be addressed below"
                    : "it is not a member of Chronicle's event context");
        }

        var members = resolution.Members.ToList();
        if (members.Count > 1 && members[^1].Name == ConceptValue && members[^2].Kind == EventContextMemberKind.Value)
        {
            members.RemoveAt(members.Count - 1);
        }

        var canonical = string.Join('.', members.Select(member => member.Name));
        var leaf = members[^1];
        if (members.Take(members.Count - 1).Any(member => member.Name == OnBehalfOf))
        {
            return SemanticEventContextScalar.Rejected(canonical, $"'{OnBehalfOf}' is optional and Chronicle does not read a missing one as null");
        }

        switch (leaf.Kind)
        {
            case EventContextMemberKind.Collection:
                return SemanticEventContextScalar.Rejected(canonical, "it is a collection, not one value");
            case EventContextMemberKind.Composite:
                return SemanticEventContextScalar.Rejected(canonical, $"it has members rather than one value - name one of {string.Join(", ", EventContextCatalog.MembersOf(leaf).Select(member => member.Name))}");
            case EventContextMemberKind.Function:
                return SemanticEventContextScalar.Rejected(canonical, $"'{leaf.Name}' is a function Chronicle computes, and ESM v1 has no derived-value shape");
        }

        if (canonical == EventSourceId)
        {
            return new(canonical, SemanticEventContextScalarKind.EventSource, SemanticPrimitiveType.Unknown, string.Empty);
        }

        var underlying = EventContextCatalog.MembersOf(leaf) is [{ Name: ConceptValue } value] ? value.Type : leaf.Type;
        return _primitives.TryGetValue(underlying, out var primitive)
            ? new(canonical, SemanticEventContextScalarKind.Scalar, primitive, string.Empty)
            : SemanticEventContextScalar.Rejected(canonical, $"its type {leaf.Type} has no portable primitive");
    }
}
