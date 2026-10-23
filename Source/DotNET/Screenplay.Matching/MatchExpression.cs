// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching;

internal abstract record MatchExpression
{
    internal static readonly MatchExpression Empty = new MatchSequenceExpression([]);

    internal static MatchExpression Sequence(List<MatchExpression> items) => items.Count switch
    {
        0 => Empty,
        1 => items[0],
        _ => new MatchSequenceExpression([.. items])
    };

    internal static MatchExpression Choice(List<MatchExpression> alternatives)
    {
        // Empty branches and nested choices must not multiply epsilon work during repeat expansion.
        var flattened = alternatives.SelectMany(alternative => alternative is MatchChoiceExpression choice ? choice.Alternatives.AsEnumerable() : [alternative]).Distinct().ToArray();

        return flattened.Length == 1 ? flattened[0] : new MatchChoiceExpression([.. flattened]);
    }
}
