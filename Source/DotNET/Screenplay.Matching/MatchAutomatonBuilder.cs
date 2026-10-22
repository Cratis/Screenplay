// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching;

// Thompson construction in continuation order. No input-dependent backtracking or determinization.
// For expanded size S, Build adds at most S states: one per character/assertion and choice point,
// none for sequences/groups, and repeats copy only their budgeted bodies and choices. Flattening
// choices and reusing the + body can only reduce that count. Compile adds one Accept state, so
// StateCount <= S + 1 <= 2 * S (k = 2 for S >= 1). S = 0 has exactly the single Accept state.
internal sealed class MatchAutomatonBuilder
{
    readonly List<MatchInstruction> _states = [];

    internal PortableMatchMachine Compile(MatchExpression expression)
    {
        var accept = Add(new(MatchInstructionKind.Accept));
        var start = Build(expression, accept);

        return new([.. _states], start);
    }

    int Build(MatchExpression expression, int next) => expression switch
    {
        MatchCharacterExpression character => Add(new(MatchInstructionKind.Character, next, Characters: character.Characters)),
        MatchAssertionExpression assertion => Add(new(assertion.IsStart ? MatchInstructionKind.Start : MatchInstructionKind.End, next)),
        MatchSequenceExpression sequence => Sequence(sequence, next),
        MatchChoiceExpression choice => Choice(choice, next),
        MatchRepeatExpression repeat => Repeat(repeat, next),
        _ => throw new InvalidOperationException("Unknown portable match expression.")
    };

    int Sequence(MatchSequenceExpression sequence, int next)
    {
        for (var index = sequence.Items.Length - 1; index >= 0; index--) next = Build(sequence.Items[index], next);

        return next;
    }

    int Choice(MatchChoiceExpression choice, int next)
    {
        var start = Build(choice.Alternatives[0], next);
        foreach (var alternative in choice.Alternatives.Skip(1)) start = Add(new(MatchInstructionKind.Epsilon, start, Build(alternative, next)));

        return start;
    }

    int Repeat(MatchRepeatExpression repeat, int next)
    {
        if (repeat.Maximum is not { } maximum) return Unbounded(repeat, next);
        for (var count = repeat.Minimum; count < maximum; count++) next = Add(new(MatchInstructionKind.Epsilon, Build(repeat.Body, next), next));
        for (var count = 0; count < repeat.Minimum; count++) next = Build(repeat.Body, next);

        return next;
    }

    int Unbounded(MatchRepeatExpression repeat, int next)
    {
        var loop = Add(new(MatchInstructionKind.Epsilon, Alternative: next));
        var body = Build(repeat.Body, loop);
        _states[loop] = _states[loop] with { Next = body };

        // Reuse the body's states for +; {1,} may also reuse them, below its conservative budget.
        if (repeat.Minimum == 1) return body;
        var start = loop;
        for (var count = 0; count < repeat.Minimum; count++) start = Build(repeat.Body, start);

        return start;
    }

    int Add(MatchInstruction state)
    {
        _states.Add(state);

        return _states.Count - 1;
    }
}
