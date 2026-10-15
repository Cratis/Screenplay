// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// Pike VM without captures. State arrays are immutable; each call owns its two sparse work sets.
internal sealed class PortableMatchMachine(MatchInstruction[] states, int start)
{
    internal int StateCount => states.Length;

    internal bool IsMatch(string input)
    {
        var current = new MatchStateSet(states.Length);
        var next = new MatchStateSet(states.Length);
        for (var position = 0; position <= input.Length; position++)
        {
            // Start assertions guard themselves. Restarting also handles mixed alternatives such as ^a|b.
            current.Add(start);
            for (var index = 0; index < current.Count; index++)
            {
                var state = states[current[index]];
                switch (state.Kind)
                {
                    case MatchInstructionKind.Accept: return true;
                    case MatchInstructionKind.Epsilon:
                        current.Add(state.Next);
                        current.Add(state.Alternative);
                        break;
                    case MatchInstructionKind.Start:
                        if (position == 0) current.Add(state.Next);
                        break;
                    case MatchInstructionKind.End:
                        if (position == input.Length) current.Add(state.Next);
                        break;
                    case MatchInstructionKind.Character:
                        if (position < input.Length && state.Characters!.Contains(input[position])) next.Add(state.Next);
                        break;
                }
            }
            current.Clear();
            (current, next) = (next, current);
        }

        return false;
    }
}
