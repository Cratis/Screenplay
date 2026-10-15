// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed class GenerationWorkBudget(long limit = GenerationWorkBudget.OperationLimit)
{
    internal const int PositionLimit = 2_048;
    internal const int StateLimit = 10_000;
    internal const long OperationLimit = 100_000_000;
    internal long Used { get; private set; }

    // One unit per inspected edge, counting-state character transition, or assessed UTC date.
    internal void Spend(long operations = 1)
    {
        if (operations < 0 || operations > limit - Used) throw new GenerationSupportBudgetExceeded($"operation budget ({limit} operations)");
        Used += operations;
    }
}
