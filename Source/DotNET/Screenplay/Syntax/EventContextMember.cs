// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents one member an <c>$eventContext.&lt;path&gt;</c> segment can name.
/// </summary>
/// <param name="Name">The name as written in a projection - camelCase for a property, as registered for a function.</param>
/// <param name="Type">The name of the type the member holds; <see cref="EventContextCatalog.MembersOf(EventContextMember)"/> gives what lies below it.</param>
/// <param name="Kind">The <see cref="EventContextMemberKind"/> of the member.</param>
/// <param name="Description">A one-line description of the member.</param>
public record EventContextMember(string Name, string Type, EventContextMemberKind Kind, string Description)
{
    /// <summary>
    /// Gets the name of the member as the runtime declares it - PascalCase for a property.
    /// </summary>
    public string DeclaredName => Kind == EventContextMemberKind.Function ? Name : $"{char.ToUpperInvariant(Name[0])}{Name[1..]}";

    /// <summary>
    /// Determines whether a path segment names this member.
    /// </summary>
    /// <param name="segment">The path segment as written.</param>
    /// <returns><see langword="true"/> when the segment names this member; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Mirrors how Chronicle resolves a segment: a property matches its declared name exactly or with only its first
    /// character uppercased (<c>Chronicle/Source/Infrastructure/Properties/PropertyPath.cs:325-326</c>), so both
    /// <c>eventType</c> and <c>EventType</c> resolve. A function is looked up ordinally, with optional parentheses
    /// (<c>PropertyPath.cs:378-381</c>, <c>DerivedPropertyFunctions.cs:43-48</c>), so <c>Week</c> and
    /// <c>Week()</c> resolve and <c>week</c> does not.
    /// </remarks>
    public bool IsNamedBy(string segment) => Kind == EventContextMemberKind.Function
        ? segment == Name || segment == $"{Name}()"
        : segment == DeclaredName || (segment.Length > 0 && $"{char.ToUpperInvariant(segment[0])}{segment[1..]}" == DeclaredName);
}
