// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed class RegexDeterminization(GlushkovAutomaton automaton)
{
    internal const int StateBudget = 10_000;
    readonly Dictionary<string, int> _identities = [];
    readonly List<int[]> _states = [];
    readonly Dictionary<(int State, char Character), int> _transitions = [];

    internal int Start => Intern([0]);

    internal bool Accepts(int state) => automaton.Accepts(_states[state]);

    internal int Move(int state, char character)
    {
        if (_transitions.TryGetValue((state, character), out var target)) return target;
        target = Intern(automaton.Move(_states[state], character));
        _transitions.Add((state, character), target);

        return target;
    }

    int Intern(int[] state)
    {
        var key = string.Join(',', state.Select(position => position.ToString(CultureInfo.InvariantCulture)));
        if (_identities.TryGetValue(key, out var identity)) return identity;
        if (_states.Count == StateBudget) throw new RegexStateBudgetExceeded();
        identity = _states.Count;
        _states.Add(state);
        _identities.Add(key, identity);

        return identity;
    }
}
