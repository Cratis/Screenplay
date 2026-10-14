// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed partial class RegexFragmentR
{
    internal const int GroupNestingBudget = 64;

    // Preflight before recursive descent or runtime regex construction, including outside-fragment groups.
    void ValidateNesting()
    {
        var depth = 0;
        var inClass = false;
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character == '\\')
            {
                index++;
                continue;
            }

            if (character == '[')
            {
                inClass = true;
            }
            else if (character == ']')
            {
                inClass = false;
            }
            else if (!inClass && character == '(')
            {
                if (++depth > GroupNestingBudget) throw new RegexStateBudgetExceeded("group nesting budget exceeded");
            }
            else if (!inClass && character == ')' && depth > 0)
            {
                depth--;
            }
        }
    }
}
