// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Lists the leading names a lifecycle condition reads.
/// </summary>
internal static class ConditionReads
{
    internal static IEnumerable<string> Heads(ConditionSyntax condition) => condition switch
    {
        LogicalConditionSyntax logical => Heads(logical.Left).Concat(Heads(logical.Right)),
        ComparisonConditionSyntax comparison => Head(comparison.Left).Concat(comparison.Right is PathExpressionSyntax path ? Head(path.Path) : []),
        _ => []
    };

    static IEnumerable<string> Head(string text) =>
        text.Length > 0 && (char.IsLetter(text[0]) || text[0] == '_') ? [text.Split('.')[0]] : [];
}
