// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticReactionLoop
{
    readonly Dictionary<SemanticId, Dictionary<SemanticValue, SemanticCaptureRecord>> _captureStates = [];

    internal SemanticExecutionResult? FoldCaptures()
    {
        foreach (var fact in World.Facts)
        {
            if (DeliverCaptures(fact, fact.Occurred, history: true) is { } failure) return failure;
        }

        return null;
    }

    SemanticExecutionResult? DeliverCaptures(SemanticFact fact, DateTimeOffset? occurred, bool history)
    {
        foreach (var capture in plan.Captures.Values.Where(capture => capture.EventsSource?.Events.Contains(fact.EventContract) == true).OrderBy(capture => capture.Id.ToString(), StringComparer.Ordinal))
        {
            var contract = plan.Events[fact.EventContract];
            var record = SemanticCaptureEvaluation.EventRecord(plan, contract, fact);
            var key = SemanticCaptureEvaluation.EventKey(capture, contract, record);
            if (capture.EventsSource is { KeyIsEventSource: true } && SemanticCaptureEvaluation.KeyDisagreesWithSource(fact, key) is { } disagreement)
            {
                throw new InvalidSemanticContract($"Capture '{capture.Name}' keys by event source, but event '{contract.Name}' from source '{disagreement}' carries key '{SemanticCaptureEvaluation.KeyText(key)}'.");
            }
            if (key is not (SemanticTextValue or SemanticNumberValue))
            {
                return new SemanticUnsupported(World, SemanticExecutionCapability.Specification, $"Capture '{capture.Name}' requires a text or numeric event key '{capture.Key}'.");
            }
            if (!_captureStates.TryGetValue(capture.Id, out var states))
            {
                states = [];
                _captureStates.Add(capture.Id, states);
            }
            var previous = states.GetValueOrDefault(key);
            var current = SemanticCaptureEvaluation.Overlay(previous, record);
            states[key] = current;
            if (history) continue;
            var occurrence = occurred is { } at ? new SemanticCommandOccurrence(at, string.Empty, string.Empty, string.Empty) { IsTimeOnly = true } : null;
            var result = SemanticCaptureEvaluation.EvaluateEvent(plan, capture, key, previous, current, occurrence, appended => Append(appended, occurred) ?? Settle());
            if (result.Unsupported is { } reason) return new SemanticUnsupported(World, SemanticExecutionCapability.Specification, reason);
            if (result.Failure is { } failure) return failure;
        }

        return null;
    }
}
