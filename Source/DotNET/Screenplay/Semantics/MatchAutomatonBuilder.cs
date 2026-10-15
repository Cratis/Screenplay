// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// Thompson construction in continuation order. No input-dependent backtracking or determinization.
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
        _ => throw new InvalidSemanticContract("Unknown portable match expression.")
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

        // Reuse the body's states for +, whose admission size counts its body only once.
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
