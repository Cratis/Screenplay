// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed class GenerationPatternAutomaton
{
    readonly List<List<(string? Characters, int Target)>> _edges = [[]];
    readonly List<int[]> _states = [];
    readonly Dictionary<string, int> _identities = new(StringComparer.Ordinal);
    readonly Dictionary<(int State, char Character), int> _moves = [];
    readonly GenerationWorkBudget _budget;
    readonly int _final;

    internal GenerationPatternAutomaton(GenerationPattern pattern, DateOnly? date, bool reverse, GenerationWorkBudget budget)
    {
        _budget = budget;
        if (pattern.Tokens.Sum(token => token.Width * token.Maximum) > GenerationWorkBudget.PositionLimit) throw new GenerationSupportBudgetExceeded("position budget (2048 character positions)");
        var current = 0;
        var positions = 0;
        foreach (var token in pattern.Tokens)
        {
            var finish = Node();
            var characters = token.Expand(date);
            for (var repetition = 1; repetition <= token.Maximum; repetition++)
            {
                foreach (var alphabet in characters)
                {
                    if (positions == GenerationWorkBudget.PositionLimit) throw new GenerationSupportBudgetExceeded("position budget (2048 character positions)");
                    positions++;
                    var next = Node();
                    _edges[current].Add((alphabet, next));
                    current = next;
                }
                if (repetition >= token.Minimum) _edges[current].Add((null, finish));
            }
            current = finish;
        }
        _final = reverse ? 0 : current;
        if (reverse) Reverse();
        Start = Intern(Closure([reverse ? current : 0]));
    }

    internal int Start { get; }
    internal bool Accepts(int state) => _states[state].Contains(_final);
    internal bool IsEmpty(int state) => _states[state].Length == 0;

    internal int Move(int state, char character)
    {
        if (_moves.TryGetValue((state, character), out var target)) return target;
        var next = new HashSet<int>();
        foreach (var position in _states[state])
        {
            foreach (var edge in _edges[position])
            {
                _budget.Spend();
                if (edge.Characters?.Contains(character, StringComparison.Ordinal) == true) next.Add(edge.Target);
            }
        }
        target = Intern(Closure(next));
        _moves.Add((state, character), target);

        return target;
    }

    int Node()
    {
        _edges.Add([]);

        return _edges.Count - 1;
    }

    void Reverse()
    {
        var reversed = _edges.Select(_ => new List<(string? Characters, int Target)>()).ToArray();
        for (var position = 0; position < _edges.Count; position++)
        {
            foreach (var edge in _edges[position])
            {
                _budget.Spend();
                reversed[edge.Target].Add((edge.Characters, position));
            }
        }
        for (var position = 0; position < _edges.Count; position++) _edges[position] = reversed[position];
    }

    int[] Closure(IEnumerable<int> positions)
    {
        var reached = new HashSet<int>(positions);
        var pending = new Stack<int>(reached);
        while (pending.TryPop(out var position))
        {
            foreach (var edge in _edges[position])
            {
                _budget.Spend();
                if (edge.Characters is null && reached.Add(edge.Target)) pending.Push(edge.Target);
            }
        }

        return [.. reached.Order()];
    }

    int Intern(int[] positions)
    {
        var key = string.Join(',', positions.Select(position => position.ToString(CultureInfo.InvariantCulture)));
        if (_identities.TryGetValue(key, out var state)) return state;
        if (_states.Count == GenerationWorkBudget.StateLimit) throw new GenerationSupportBudgetExceeded("state budget (10000 distinct DFA/product states)");
        state = _states.Count;
        _identities.Add(key, state);
        _states.Add(positions);

        return state;
    }
}
