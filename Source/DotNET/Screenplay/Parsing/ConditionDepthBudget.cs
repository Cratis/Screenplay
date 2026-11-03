// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing;

/// <summary>Tracks supported nesting and constructed condition depth for one requirement.</summary>
internal sealed class ConditionDepthBudget(ParserContext context, SourceLocation location)
{
    const int Limit = 128;
    readonly Dictionary<object, int> _depths = new(ReferenceEqualityComparer.Instance);
    int _nesting;

    public bool Exceeded { get; private set; }

    public bool Enter()
    {
        if (_nesting == Limit) return Refuse();
        _nesting++;
        return true;
    }

    public void Leave() => _nesting--;

    public T? Build<T>(Func<T> create, params object[] children)
        where T : class
    {
        var depth = 1 + children.Max(child => _depths.GetValueOrDefault(child, 1));
        if (!AllowsDepth(depth)) return null;

        var node = create();
        _depths.Add(node, depth);
        return node;
    }

    public bool AllowsDepth(int depth) => depth <= Limit || Refuse();

    bool Refuse()
    {
        if (!Exceeded)
        {
            context.Error(DiagnosticCodes.ConditionDepthExceeded, $"Condition nesting or expression depth exceeds the supported limit of {Limit}.", location);
            Exceeded = true;
        }

        return false;
    }
}
