// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal static partial class SemanticCaptureEvaluation
{
    // A list of objects becomes child records and an object a nested record, so a capture over events can fan out and
    // narrow with 'children' and 'nested' like any record. The whole value stays beside them to be copied.
    internal static SemanticCaptureRecord EventRecord(SemanticExecutionPlan plan, SemanticEventContract contract, SemanticFact fact)
    {
        var names = plan.Model.Application.Types.SelectMany(type => type.Properties).ToDictionary(property => property.Id, property => property.Name);
        return new([.. fact.Values.Select(value => EventField(contract.Properties.Single(property => property.Id == value.TargetProperty).Name, value.Value, names))]);
    }

    static SemanticCaptureField EventField(string name, SemanticValue value, Dictionary<SemanticId, string> names) => value switch
    {
        SemanticArrayValue { Values: { IsDefaultOrEmpty: false } elements } when elements.All(element => element is SemanticCompositeValue) =>
            new(name, SemanticCaptureFieldKind.Records) { Value = value, Records = [.. elements.Cast<SemanticCompositeValue>().Select(element => EventObject(element, names))] },
        SemanticCompositeValue composite =>
            new(name, SemanticCaptureFieldKind.Record) { Value = value, Record = EventObject(composite, names) },
        _ => Scalar(name, value)
    };

    static SemanticCaptureRecord EventObject(SemanticCompositeValue composite, Dictionary<SemanticId, string> names) =>
        new([.. composite.Properties.Select(property => EventField(names[property.TargetProperty], property.Value, names))]);

    internal static SemanticValue EventKey(SemanticCapture capture, SemanticEventContract contract, SemanticCaptureRecord record)
    {
        var key = record.Fields.SingleOrDefault(field => string.Equals(field.Name, capture.Key, StringComparison.Ordinal))?.Value;
        if (key is null or SemanticNullValue || (key is SemanticTextValue text && string.IsNullOrWhiteSpace(text.Value)))
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
        if (previous is not null && SameEventState(plan, previous, current)) return new(null, null);

        var context = new Context(plan, capture, key, occurrence, accept, eventsSource: true);
        ValidatePreconditions(context, capture, previous, current);
        return EvaluateScopes(context, capture, previous, current);
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
