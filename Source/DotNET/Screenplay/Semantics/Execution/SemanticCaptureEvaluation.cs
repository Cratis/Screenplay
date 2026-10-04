// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Evaluates a capture against the record it last saw and the record it is presented now.
/// </summary>
/// <remarks>
/// <para>
/// Map operations apply in order, each to the record as the ones before it left it, and conditions compare the mapped
/// records. A field the earlier record lacked has changed when the new one has it, so a first record has changed in
/// every field it carries. <c>$.field</c> reads the item being evaluated, and the enclosing record when the item lacks it.
/// </para>
/// <para>
/// The record's own appends run first, then each child collection's - current children in order, then the removed
/// ones in their earlier order - then each nested record's. Everything is appended to the event source the record key
/// names.
/// </para>
/// </remarks>
internal static class SemanticCaptureEvaluation
{
    /// <summary>
    /// Works out the facts a capture appends.
    /// </summary>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="capture">The capture.</param>
    /// <param name="previous">The record the capture last saw for the key, if any.</param>
    /// <param name="current">The record presented now.</param>
    /// <param name="occurrence">The occurrence, when the scenario states a clock.</param>
    /// <returns>The facts in append order.</returns>
    /// <exception cref="InvalidSemanticContract">A value does not fit the event it is appended to.</exception>
    public static ImmutableArray<SemanticFact> Evaluate(
        SemanticExecutionPlan plan,
        SemanticCapture capture,
        SemanticCaptureRecord? previous,
        SemanticCaptureRecord current,
        SemanticCommandOccurrence? occurrence)
    {
        var key = Scalars(current).GetValueOrDefault(capture.Key)
            ?? throw new InvalidSemanticContract($"The record presented to capture '{capture.Name}' has no key '{capture.Key}'.");
        var context = new Context(plan, key, occurrence);
        var before = previous is null ? null : Map(Scalars(previous), capture.Map);
        var after = Map(Scalars(current), capture.Map);
        var facts = ImmutableArray.CreateBuilder<SemanticFact>();
        facts.AddRange(context.Appends(capture.Appends, before, after, null));
        foreach (var children in capture.Children)
        {
            var was = Records(previous, children.Field);
            var now = Records(current, children.Field);
            var wasById = was.ToDictionary(record => Identity(record, children.IdentifiedBy), StringComparer.Ordinal);
            foreach (var child in now)
            {
                var earlier = wasById.GetValueOrDefault(Identity(child, children.IdentifiedBy));
                facts.AddRange(context.Appends(children.Appends, earlier is null ? null : Map(Scalars(earlier), children.Map), Map(Scalars(child), children.Map), after));
            }

            var nowIds = now.Select(record => Identity(record, children.IdentifiedBy)).ToHashSet(StringComparer.Ordinal);
            foreach (var removed in was.Where(record => !nowIds.Contains(Identity(record, children.IdentifiedBy))))
            {
                facts.AddRange(context.Appends(children.Appends, Map(Scalars(removed), children.Map), null, after));
            }
        }

        foreach (var nested in capture.Nested)
        {
            var was = Nested(previous, nested.Field);
            var now = Nested(current, nested.Field);
            if (was is null && now is null) continue;
            facts.AddRange(context.Appends(nested.Appends, was is null ? null : Map(Scalars(was), nested.Map), now is null ? null : Map(Scalars(now), nested.Map), after));
        }

        return facts.ToImmutable();
    }

    static Dictionary<string, SemanticValue> Scalars(SemanticCaptureRecord record) =>
        record.Fields.Where(field => field.Kind == SemanticCaptureFieldKind.Value)
            .ToDictionary(field => field.Name, field => field.Value!, StringComparer.Ordinal);

    static ImmutableArray<SemanticCaptureRecord> Records(SemanticCaptureRecord? record, string field) =>
        record?.Fields.SingleOrDefault(value => value.Name == field && value.Kind == SemanticCaptureFieldKind.Records)?.Records ?? [];

    static SemanticCaptureRecord? Nested(SemanticCaptureRecord? record, string field) =>
        record?.Fields.SingleOrDefault(value => value.Name == field && value.Kind == SemanticCaptureFieldKind.Record)?.Record;

    static string Identity(SemanticCaptureRecord record, string field) => SemanticValueRules.Text(Scalars(record).GetValueOrDefault(field));

    static Dictionary<string, SemanticValue> Map(Dictionary<string, SemanticValue> record, ImmutableArray<SemanticCaptureMap> map)
    {
        var values = new Dictionary<string, SemanticValue>(record, StringComparer.Ordinal);
        foreach (var operation in map)
        {
            switch (operation.Kind)
            {
                case SemanticCaptureMapKind.Value:
                    values[operation.Targets[0]] = Translate(values.GetValueOrDefault(operation.Source!) ?? SemanticValue.Null, operation.Translations);
                    break;
                case SemanticCaptureMapKind.Template:
                    var text = string.Concat(operation.Template.Select(part => part.Text ?? SemanticValueRules.Text(values.GetValueOrDefault(part.Field!))));
                    values[operation.Targets[0]] = Translate(SemanticValue.Text(text), operation.Translations);
                    break;
                default:
                    var parts = SemanticValueRules.Text(values.GetValueOrDefault(operation.Source!))
                        .Split(operation.Separator, operation.Targets.Length, StringSplitOptions.TrimEntries);
                    for (var index = 0; index < operation.Targets.Length; index++)
                    {
                        values[operation.Targets[index]] = index < parts.Length ? SemanticValue.Text(parts[index]) : SemanticValue.Null;
                    }

                    break;
            }
        }

        return values;
    }

    static SemanticValue Translate(SemanticValue value, ImmutableArray<SemanticCaptureTranslation> translations) =>
        translations.FirstOrDefault(translation => translation.From == SemanticValueRules.Text(value)) is { } match
            ? SemanticValue.Text(match.To)
            : value;

    sealed class Context(SemanticExecutionPlan plan, SemanticValue key, SemanticCommandOccurrence? occurrence)
    {
        readonly SemanticValueValidator _validator = new(
            plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
            plan.Model.Application.Types.ToDictionary(type => type.Id));

        public IEnumerable<SemanticFact> Appends(
            ImmutableArray<SemanticCaptureAppend> appends,
            Dictionary<string, SemanticValue>? before,
            Dictionary<string, SemanticValue>? after,
            Dictionary<string, SemanticValue>? enclosing)
        {
            var item = after ?? before!;
            foreach (var append in appends.Where(append => Holds(append.When, before, after)))
            {
                var eventContract = plan.Events[append.EventContract];
                var properties = eventContract.Properties.ToDictionary(property => property.Id);
                var values = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
                foreach (var mapping in append.Mappings)
                {
                    var property = properties[mapping.TargetProperty];
                    var value = mapping.Field is { } field
                        ? Coerce(item.GetValueOrDefault(field) ?? enclosing?.GetValueOrDefault(field) ?? SemanticValue.Null, property.Type)
                        : Value(mapping.Value!);
                    _validator.Validate(value, property.Type, $"event property '{property.Name}'");
                    values.Add(new(property.Id, value));
                }

                _validator.Validate(key, append.EventSourceType, "capture event source");
                yield return new SemanticFact(append.EventContract, key, values.ToImmutable())
                {
                    Context = new(new(append.EventSourceType, key)),
                    Tags = eventContract.Tags.AddRange(append.Tags)
                };
            }
        }

        static bool Holds(SemanticCaptureCondition? when, Dictionary<string, SemanticValue>? before, Dictionary<string, SemanticValue>? after) => when?.Kind switch
        {
            null => after is not null,
            SemanticCaptureConditionKind.Added => before is null && after is not null,
            SemanticCaptureConditionKind.Removed => before is not null && after is null,
            _ when after is null => false,
            SemanticCaptureConditionKind.AnyChanged => when.Fields.Any(field => Changed(before, after, field)),
            SemanticCaptureConditionKind.AllChanged => when.Fields.All(field => Changed(before, after, field)),
            SemanticCaptureConditionKind.Transition => before is not null &&
                SemanticValueRules.Text(before.GetValueOrDefault(when.Fields[0])) == when.From &&
                SemanticValueRules.Text(after.GetValueOrDefault(when.Fields[0])) == when.To,
            SemanticCaptureConditionKind.Expression => SemanticCaptureExpression.TryParse(when.Expression, out var expression) &&
                SemanticCaptureExpression.Evaluate(expression, field => after.GetValueOrDefault(field)),
            _ => throw new InvalidSemanticContract($"Capture condition '{when.Kind}' is unknown.")
        };

        static bool Changed(Dictionary<string, SemanticValue>? before, Dictionary<string, SemanticValue> after, string field) =>
            before is null
                ? after.ContainsKey(field)
                : before.TryGetValue(field, out var was) != after.TryGetValue(field, out var now) ||
                  (was is not null && now is not null && !SemanticValueRules.AreEqual(was, now));

        SemanticValue Value(SemanticExpression expression) => expression switch
        {
            SemanticValueExpression literal => literal.Value,
            SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } when occurrence is not null =>
                SemanticValue.Text(occurrence.Occurred.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)),
            SemanticEventContextExpression => throw new InvalidSemanticContract("A capture using $context.occurred needs the scenario's 'given clock'."),
            _ => throw new InvalidSemanticContract("A capture mapping expression is unknown.")
        };

        // A record carries no types, so an instant arrives the way the source wrote it; an instant bound for a DateTime
        // property takes its round-trip form, the way a specification's own instants do.
        SemanticValue Coerce(SemanticValue value, SemanticTypeReference type)
        {
            var primitive = type.Kind switch
            {
                SemanticTypeReferenceKind.Primitive => type.Primitive,
                SemanticTypeReferenceKind.Concept => plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Primitive,
                _ => SemanticPrimitiveType.Unknown
            };
            return primitive == SemanticPrimitiveType.DateTime && value is SemanticTextValue text &&
                DateTimeOffset.TryParse(text.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var instant)
                    ? SemanticValue.Text(instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
                    : value;
        }
    }
}
