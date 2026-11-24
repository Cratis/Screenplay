// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Screenplay.Syntax.Serialization;

internal static class SyntaxJsonDefaults
{
    internal static bool Omit(SyntaxNode node, string member, object? value) =>
        ClockSyntaxDefaults.Omit(node, member, value) ||
        InvocationSyntaxDefaults.Omit(node, member, value) ||
        CompositionDefaults.Omit(node, member, value) ||
        OmitScreenDefaults(node, member, value) ||
        OmitTagExpectations(node, member, value);

    static bool OmitScreenDefaults(SyntaxNode node, string member, object? value) =>
        OmitAbsentReference(node, member, value) || OmitInputs(node, member, value) || OmitPresentation(node, member, value) || OmitElementGates(node, member, value);

    // A screen action or data element with no gate of its own writes no member, so every syntax that predates element gates keeps its bytes.
    static bool OmitElementGates(SyntaxNode node, string member, object? value) =>
        node is ScreenActionSyntax or ScreenDataSyntax && member == "available" && value is IEnumerable<AvailableSyntax> gates && !gates.Any();

    static bool OmitPresentation(SyntaxNode node, string member, object? value) =>
        node is ScreenActionSyntax && member == "presentation" && value is IEnumerable<PresentationValueSyntax> presentation && !presentation.Any();

    static bool OmitAbsentReference(SyntaxNode node, string member, object? value) =>
        value is null && (node, member) is
            (ScreenActionSyntax or ScreenGuardedActionSyntax or ToolbarItemSyntax, "subject") or
            (NavigationTargetSyntax, "screen");

    static bool OmitInputs(SyntaxNode node, string member, object? value) =>
        node is ScreenActionSyntax && member == "inputs" && value is IEnumerable<InteractionArgumentSyntax> inputs && !inputs.Any();

    static bool OmitTagExpectations(SyntaxNode node, string member, object? value)
    {
        if (node is not Specifications.SpecificationEventSyntax) return false;
        return member switch
        {
            "noTags" => value is null,
            "tags" => value is IEnumerable tags && !tags.Cast<object>().Any(),
            _ => false
        };
    }
}
