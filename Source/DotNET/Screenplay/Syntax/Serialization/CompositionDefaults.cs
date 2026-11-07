// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Syntax.Serialization;

/// <summary>
/// Keeps the canonical JSON of documents without screen composition byte-identical to what it was before the
/// composition members existed: a member at its default is omitted, and reads back as that default.
/// </summary>
static class CompositionDefaults
{
    static readonly HashSet<string> _optionalBooleans = new(StringComparer.Ordinal) { nameof(LayoutSyntax.RestrictsScopes), nameof(Captures.CaptureSyntax.KeyIsEventSource), nameof(OutletItemSyntax.IsDefault), nameof(SlotSyntax.Routed), nameof(SlotSyntax.Primary), nameof(SlotSyntax.Optional) };

    static readonly Dictionary<string, Default> _templateMembers = new(StringComparer.Ordinal)
    {
        ["restrictsScopes"] = Default.False,
        ["scopes"] = Default.Empty,
        ["sliceKinds"] = Default.Empty,
        ["content"] = Default.Empty,
        ["displayName"] = Default.Null,
        ["description"] = Default.Null,
        ["outlets"] = Default.Empty
    };

    static readonly Dictionary<string, Default> _outletItemMembers = new(StringComparer.Ordinal) { ["isDefault"] = Default.False, ["position"] = Default.Null };

    static readonly Dictionary<Type, Dictionary<string, Default>> _omittable = new()
    {
        [typeof(ApplicationSyntax)] = new(StringComparer.Ordinal) { ["exposures"] = Default.Empty, ["instanceContributions"] = Default.Empty, ["stickyNotes"] = Default.Empty, ["users"] = Default.Empty, ["stateMachines"] = Default.Empty, ["workflows"] = Default.Empty },
        [typeof(PersonaSyntax)] = new(StringComparer.Ordinal) { ["goals"] = Default.Empty, ["x"] = Default.Null, ["y"] = Default.Null, ["width"] = Default.Null, ["height"] = Default.Null },
        [typeof(SeedSyntax)] = new(StringComparer.Ordinal) { ["name"] = Default.Null, ["x"] = Default.Null, ["y"] = Default.Null },
        [typeof(EventSourceSyntax)] = new(StringComparer.Ordinal) { ["x"] = Default.Null, ["y"] = Default.Null },
        [typeof(ScreenSyntax)] = new(StringComparer.Ordinal) { ["contributions"] = Default.Empty, ["outlets"] = Default.Empty },
        [typeof(ScreenNavigateSyntax)] = new(StringComparer.Ordinal) { ["slot"] = Default.Null },
        [typeof(SlotSyntax)] = new(StringComparer.Ordinal) { ["role"] = Default.Null, ["routed"] = Default.False, ["routedBy"] = Default.Null, ["primary"] = Default.False, ["optional"] = Default.False },
        [typeof(ContributionSyntax)] = new(StringComparer.Ordinal) { ["id"] = Default.Null, ["icon"] = Default.Null, ["presentation"] = Default.Null, ["group"] = Default.Null, ["items"] = Default.Empty, ["destination"] = Default.Null },
        [typeof(LayoutSyntax)] = new(StringComparer.Ordinal) { ["restrictsScopes"] = Default.False, ["scopes"] = Default.Empty, ["sliceKinds"] = Default.Empty, ["outlets"] = Default.Empty },
        [typeof(TemplateAssignmentSyntax)] = new(StringComparer.Ordinal) { ["blueprint"] = Default.Null, ["version"] = Default.Null },
        [typeof(ScreenTemplateReferenceSyntax)] = new(StringComparer.Ordinal) { ["blueprint"] = Default.Null, ["version"] = Default.Null },
        [typeof(ModuleSyntax)] = new(StringComparer.Ordinal) { ["outlets"] = Default.Empty },
        [typeof(FeatureSyntax)] = new(StringComparer.Ordinal) { ["outlets"] = Default.Empty },
        [typeof(ToolbarItemSyntax)] = new(StringComparer.Ordinal) { ["confirm"] = Default.Null },
        [typeof(ScreenToolbarSyntax)] = new(StringComparer.Ordinal) { ["buttons"] = Default.Empty },
        [typeof(ToolbarButtonSyntax)] = new(_outletItemMembers, StringComparer.Ordinal) { ["slice"] = Default.Null, ["emphasis"] = Default.Null, ["icon"] = Default.Null, ["authorize"] = Default.Null, ["confirm"] = Default.Null, ["runs"] = Default.Null },
        [typeof(OutletSyntax)] = new(StringComparer.Ordinal) { ["subject"] = Default.Null, ["slot"] = Default.Null, ["hides"] = Default.Empty, ["logo"] = Default.Null },
        [typeof(OutletGroupSyntax)] = new(_outletItemMembers, StringComparer.Ordinal) { ["icon"] = Default.Null },
        [typeof(OutletFilterSyntax)] = new(_outletItemMembers, StringComparer.Ordinal) { ["query"] = Default.Null, ["parameter"] = Default.Null },
        [typeof(OutletTabSyntax)] = new(_outletItemMembers, StringComparer.Ordinal) { ["shows"] = Default.Null },
        [typeof(OutletStatSyntax)] = new(_outletItemMembers, StringComparer.Ordinal) { ["query"] = Default.Null, ["field"] = Default.Null },
        [typeof(Specifications.SpecificationSyntax)] = new(StringComparer.Ordinal) { ["givenGenerated"] = Default.Empty, ["thenInvocations"] = Default.Empty },
        [typeof(Captures.CaptureSyntax)] = new(StringComparer.Ordinal) { ["keyIsEventSource"] = Default.False },
        [typeof(AddMappingSyntax)] = new(StringComparer.Ordinal) { ["factor"] = Default.Null },
        [typeof(SubtractMappingSyntax)] = new(StringComparer.Ordinal) { ["factor"] = Default.Null },
        [typeof(NavigationItemSyntax)] = new(_outletItemMembers, StringComparer.Ordinal) { ["icon"] = Default.Null, ["target"] = Default.Null, ["slot"] = Default.Null, ["children"] = Default.Empty },
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
        (property.DeclaringType == typeof(LayoutSyntax) || property.DeclaringType == typeof(ScreenTemplateSyntax) || property.DeclaringType == typeof(DialogTemplateSyntax) || property.DeclaringType == typeof(Captures.CaptureSyntax) || property.DeclaringType == typeof(SlotSyntax) || property.DeclaringType == typeof(OutletItemSyntax));

    static bool IsEmpty(object? value) => value is null || (value is IEnumerable items && !items.Cast<object>().Any());
}
