// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal static partial class SemanticCaptureEvaluation
{
    internal static SemanticCaptureRecord EventRecord(SemanticEventContract contract, SemanticFact fact) =>
        new([.. fact.Values.Select(value => Scalar(contract.Properties.Single(property => property.Id == value.TargetProperty).Name, value.Value))]);

    internal static SemanticValue EventKey(SemanticCapture capture, SemanticEventContract contract, SemanticCaptureRecord record)
    {
        var key = record.Fields.SingleOrDefault(field => string.Equals(field.Name, capture.Key, StringComparison.Ordinal))?.Value;
        if (key is null or SemanticNullValue || key is SemanticTextValue text && string.IsNullOrWhiteSpace(text.Value))
        {
            throw new InvalidSemanticContract($"Capture '{capture.Name}' received event '{contract.Name}' with an empty key '{capture.Key}'.");
        }

        return key;
    }

    internal static SemanticCaptureRecord Overlay(SemanticCaptureRecord? previous, SemanticCaptureRecord current)
    {
        var fields = previous is null ? new Dictionary<string, SemanticCaptureField>(StringComparer.Ordinal) : Fields(previous);
        foreach (var field in current.Fields) fields[field.Name] = field;
        return new([.. fields.Values]);
    }

    internal static SemanticCaptureEvaluationResult EvaluateEvent(
        SemanticExecutionPlan plan,
        SemanticCapture capture,
        SemanticValue key,
        SemanticCaptureRecord? previous,
        SemanticCaptureRecord current,
        SemanticCommandOccurrence? occurrence,
        Func<SemanticFact, SemanticExecutionResult?> accept)
    {
        if (key is not (SemanticTextValue or SemanticNumberValue))
        {
            return new($"Capture '{capture.Name}' requires a text or numeric event key '{capture.Key}'.", null);
        }
        if (!capture.Children.IsDefaultOrEmpty || !capture.Nested.IsDefaultOrEmpty)
        {
            return new($"Capture '{capture.Name}' over events has children or nested scopes the reference evaluator does not support.", null);
        }
        if (previous is not null && SameEventState(plan, previous, current)) return new(null, null);

        var context = new Context(plan, key, occurrence, accept, eventsSource: true);
        context.ValidateSource(capture.Appends);
        var before = previous is null ? null : Map(context, Fields(previous), capture.Map);
        if (context.Stopped) return context.Result;
        var after = Map(context, Fields(current), capture.Map);
        if (context.Stopped) return context.Result;
        context.Appends(capture.Appends, before, after, null);
        return context.Result;
    }

    static bool SameEventState(SemanticExecutionPlan plan, SemanticCaptureRecord previous, SemanticCaptureRecord current)
    {
        var before = Fields(previous);
        var propertyNames = plan.Model.Application.Types.SelectMany(type => type.Properties).ToDictionary(property => property.Id, property => property.Name);
        return before.Count == current.Fields.Length && current.Fields.All(field =>
            before.TryGetValue(field.Name, out var earlier) && SameEventValue(earlier.Value!, field.Value!, propertyNames));
    }

    static bool SameEventValue(SemanticValue previous, SemanticValue current, IReadOnlyDictionary<SemanticId, string> propertyNames)
    {
        switch ((previous, current))
        {
            case (SemanticCompositeValue before, SemanticCompositeValue after):

                // Different event contracts may use different composite types for the same incoming JSON shape.
                // Chronicle compares content names, not the compiler's type-specific property identities.
                var properties = before.Properties.ToDictionary(property => propertyNames[property.TargetProperty], property => property.Value, StringComparer.Ordinal);
                return properties.Count == after.Properties.Length && after.Properties.All(property =>
                    properties.TryGetValue(propertyNames[property.TargetProperty], out var earlier) && SameEventValue(earlier, property.Value, propertyNames));
            case (SemanticArrayValue beforeArray, SemanticArrayValue afterArray):
                return beforeArray.Values.Length == afterArray.Values.Length && beforeArray.Values.Zip(afterArray.Values).All(pair => SameEventValue(pair.First, pair.Second, propertyNames));
            default:
                return SemanticValueRules.AreEqual(previous, current);
        }
    }
}
