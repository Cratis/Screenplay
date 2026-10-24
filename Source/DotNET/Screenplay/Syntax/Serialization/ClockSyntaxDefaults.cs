// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization;

/// <summary>
/// Omits absent optional clock syntax without changing the legacy syntax JSON shape.
/// </summary>
internal static class ClockSyntaxDefaults
{
    static readonly Dictionary<Type, string> _members = new()
    {
        [typeof(ApplicationSyntax)] = "timeZone",
        [typeof(CommandSyntax)] = "calendar",
        [typeof(ComparisonConditionSyntax)] = "leftExpression",
        [typeof(Specifications.SpecificationSyntax)] = "givenTimeZone"
    };

    internal static bool Omit(SyntaxNode node, string member, object? value) =>
        value is null && _members.TryGetValue(node.GetType(), out var optional) && member == optional;
}
