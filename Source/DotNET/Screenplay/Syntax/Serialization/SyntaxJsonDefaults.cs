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
        OmitSubject(node, member, value) || OmitInputs(node, member, value);

    static bool OmitSubject(SyntaxNode node, string member, object? value) =>
        node is ScreenActionSyntax or ScreenGuardedActionSyntax or ToolbarItemSyntax && member == "subject" && value is null;

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
