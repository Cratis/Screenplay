// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace Cratis.Screenplay.Syntax.Serialization;

/// <summary>
/// Keeps the canonical JSON of documents without screen composition byte-identical to what it was before the
/// composition members existed: a member at its default is omitted, and reads back as that default.
/// </summary>
static class CompositionDefaults
{
    static readonly HashSet<string> _optionalBooleans = new(StringComparer.Ordinal) { nameof(LayoutSyntax.RestrictsScopes) };

    static readonly Dictionary<string, Default> _templateMembers = new(StringComparer.Ordinal)
    {
        ["restrictsScopes"] = Default.False,
        ["scopes"] = Default.Empty,
        ["content"] = Default.Empty,
        ["displayName"] = Default.Null,
        ["description"] = Default.Null
    };

    static readonly Dictionary<Type, Dictionary<string, Default>> _omittable = new()
    {
        [typeof(ApplicationSyntax)] = new(StringComparer.Ordinal) { ["exposures"] = Default.Empty, ["instanceContributions"] = Default.Empty },
        [typeof(ScreenSyntax)] = new(StringComparer.Ordinal) { ["contributions"] = Default.Empty },
        [typeof(ScreenNavigateSyntax)] = new(StringComparer.Ordinal) { ["outlet"] = Default.Null },
        [typeof(LayoutSyntax)] = new(StringComparer.Ordinal) { ["restrictsScopes"] = Default.False, ["scopes"] = Default.Empty },
        [typeof(ScreenTemplateSyntax)] = _templateMembers,
        [typeof(DialogTemplateSyntax)] = _templateMembers,
        [typeof(ArrangementContainerSyntax)] = new(StringComparer.Ordinal) { ["columns"] = Default.Null, ["rows"] = Default.Null, ["grow"] = Default.Null, ["span"] = Default.Null },
        [typeof(ArrangementSlotSyntax)] = new(StringComparer.Ordinal) { ["growFactor"] = Default.Null }
    };

    enum Default
    {
        Null = 0,
        False = 1,
        Empty = 2
    }

    /// <summary>
    /// Determines whether a composition member at its default value is left out of the canonical JSON.
    /// </summary>
    /// <param name="node">The node being written.</param>
    /// <param name="member">The JSON member name.</param>
    /// <param name="value">The member value.</param>
    /// <returns><see langword="true"/> when the member is omitted.</returns>
    internal static bool Omit(SyntaxNode node, string member, object? value)
    {
        if (!_omittable.TryGetValue(node.GetType(), out var members) || !members.TryGetValue(member, out var whenDefault))
        {
            return false;
        }

        return whenDefault switch
        {
            Default.Null => value is null,
            Default.False => Equals(value, false),
            _ => IsEmpty(value)
        };
    }

    /// <summary>
    /// Determines whether a non-nullable composition member may be absent from JSON.
    /// </summary>
    /// <param name="property">The member's property.</param>
    /// <returns><see langword="true"/> when an absent member reads as its default.</returns>
    internal static bool IsOptional(PropertyInfo property) =>
        _optionalBooleans.Contains(property.Name) &&
        (property.DeclaringType == typeof(LayoutSyntax) || property.DeclaringType == typeof(ScreenTemplateSyntax) || property.DeclaringType == typeof(DialogTemplateSyntax));

    static bool IsEmpty(object? value) => value is null || (value is IEnumerable items && !items.Cast<object>().Any());
}
