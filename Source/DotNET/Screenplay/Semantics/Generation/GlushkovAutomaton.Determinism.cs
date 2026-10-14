// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed partial class GlushkovAutomaton
{
    // Follow[0] is the first-position set. Compare classes on the same printable alphabet as containment.
    internal bool IsDeterministic => _follow.TrueForAll(DisjointClasses);

    bool DisjointClasses(HashSet<int> positions)
    {
        var seen = new HashSet<char>();
        foreach (var position in positions)
        {
            foreach (var character in _characters[position].Distinct().Where(character => character is >= ' ' and <= '~'))
            {
                if (!seen.Add(character)) return false;
            }
        }

        return true;
    }
}
