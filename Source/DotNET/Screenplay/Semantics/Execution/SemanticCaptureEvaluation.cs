// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

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
internal static partial class SemanticCaptureEvaluation
{
    /// <summary>
    /// Works out the facts a capture appends.
    /// </summary>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="capture">The capture.</param>
    /// <param name="previous">The record the capture last saw for the key, if any.</param>
    /// <param name="current">The record presented now.</param>
    /// <param name="occurrence">The occurrence, when the scenario states a clock.</param>
    /// <returns>The facts in append order, or the reached unsupported capture value.</returns>
    /// <exception cref="InvalidSemanticContract">A value does not fit the event it is appended to.</exception>
    public static SemanticCaptureEvaluationResult Evaluate(
        SemanticExecutionPlan plan,
        SemanticCapture capture,
        SemanticCaptureRecord? previous,
        SemanticCaptureRecord current,
        SemanticCommandOccurrence? occurrence)
    {
        var key = current.Fields.SingleOrDefault(field => field.Name == capture.Key && field.Kind == SemanticCaptureFieldKind.Value)?.Value
            ?? throw new InvalidSemanticContract($"The record presented to capture '{capture.Name}' has no key '{capture.Key}'.");
        var context = new Context(plan, key, occurrence);
        var before = previous is null ? null : Map(context, Fields(previous), capture.Map);
        var after = Map(context, Fields(current), capture.Map);
        var facts = ImmutableArray.CreateBuilder<SemanticFact>();
        facts.AddRange(context.Appends(capture.Appends, before, after, null));
        if (context.Unsupported is not null) return new([], context.Unsupported);
        foreach (var children in capture.Children)
        {
            var was = Records(previous, children.Field);
            var now = Records(current, children.Field);
            var wasById = Index(was, children.IdentifiedBy);
            var nowById = Index(now, children.IdentifiedBy);
            foreach (var child in now)
            {
                var earlier = wasById.GetValueOrDefault(Identity(child, children.IdentifiedBy));
                facts.AddRange(context.Appends(children.Appends, earlier is null ? null : Map(context, Fields(earlier), children.Map), Map(context, Fields(child), children.Map), after));
                if (context.Unsupported is not null) return new([], context.Unsupported);
            }

            foreach (var removed in was.Where(record => !nowById.ContainsKey(Identity(record, children.IdentifiedBy))))
            {
                facts.AddRange(context.Appends(children.Appends, Map(context, Fields(removed), children.Map), null, after));
                if (context.Unsupported is not null) return new([], context.Unsupported);
            }
        }

        foreach (var nested in capture.Nested)
        {
            var was = Nested(previous, nested.Field);
            var now = Nested(current, nested.Field);
            if (was is null && now is null) continue;
            facts.AddRange(context.Appends(nested.Appends, was is null ? null : Map(context, Fields(was), nested.Map), now is null ? null : Map(context, Fields(now), nested.Map), after));
            if (context.Unsupported is not null) return new([], context.Unsupported);
        }

        return new(facts.ToImmutable(), null);
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.None, 1000)]
    private static partial Regex IsoInstant();

    static Dictionary<string, SemanticCaptureField> Fields(SemanticCaptureRecord record) =>
        record.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal);

    static SemanticCaptureField Scalar(string name, SemanticValue value) => new(name, SemanticCaptureFieldKind.Value) { Value = value };

    static ImmutableArray<SemanticCaptureRecord> Records(SemanticCaptureRecord? record, string field) =>
        record?.Fields.SingleOrDefault(value => value.Name == field) switch
        {
            null => [],
            { Kind: SemanticCaptureFieldKind.Records } value => value.Records,
            _ => throw new InvalidSemanticContract($"Capture children '{field}' must be a collection of records.")
        };

    static SemanticCaptureRecord? Nested(SemanticCaptureRecord? record, string field) =>
        record?.Fields.SingleOrDefault(value => value.Name == field) switch
        {
            null => null,
            { Kind: SemanticCaptureFieldKind.Record } value => value.Record,
            { Kind: SemanticCaptureFieldKind.Value, Value: SemanticNullValue } => null,
            _ => throw new InvalidSemanticContract($"Capture nested field '{field}' must be a record.")
        };

    static SemanticValue Identity(SemanticCaptureRecord record, string field) => record.Fields.SingleOrDefault(value => value.Name == field) switch
    {
        { Kind: SemanticCaptureFieldKind.Value, Value: SemanticTextValue or SemanticNumberValue } value => value.Value,
        _ => throw new InvalidSemanticContract($"Capture child identity '{field}' must be present as text or a number.")
    };

    static Dictionary<SemanticValue, SemanticCaptureRecord> Index(ImmutableArray<SemanticCaptureRecord> records, string field)
    {
        // Scalar record equality keeps the value kind and compares decimal values independently of their scale.
        var index = new Dictionary<SemanticValue, SemanticCaptureRecord>();
        foreach (var record in records)
        {
            if (!index.TryAdd(Identity(record, field), record))
            {
                throw new InvalidSemanticContract($"Capture children have duplicate identity '{field}'.");
            }
        }

        return index;
    }

    static Dictionary<string, SemanticCaptureField> Map(Context context, Dictionary<string, SemanticCaptureField> record, ImmutableArray<SemanticCaptureMap> map)
    {
        var values = new Dictionary<string, SemanticCaptureField>(record, StringComparer.Ordinal);
        foreach (var operation in map)
        {
            switch (operation.Kind)
            {
                case SemanticCaptureMapKind.Value:
                    values[operation.Targets[0]] = Scalar(operation.Targets[0], Translate(context.Read(values, operation.Source!) ?? SemanticValue.Null, operation.Translations));
                    break;
                case SemanticCaptureMapKind.Template:
                    var text = string.Concat(operation.Template.Select(part => part.Text ?? SemanticValueRules.Text(context.Read(values, part.Field!))));
                    values[operation.Targets[0]] = Scalar(operation.Targets[0], Translate(SemanticValue.Text(text), operation.Translations));
                    break;
                default:
                    var parts = SemanticValueRules.Text(context.Read(values, operation.Source!))
                        .Split(operation.Separator, operation.Targets.Length, StringSplitOptions.TrimEntries);
                    for (var index = 0; index < operation.Targets.Length; index++)
                    {
                        values[operation.Targets[index]] = Scalar(operation.Targets[index], index < parts.Length ? SemanticValue.Text(parts[index]) : SemanticValue.Null);
                    }

                    break;
            }

            if (context.Unsupported is not null) return values;
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

        public string? Unsupported { get; private set; }

        public SemanticValue? Read(Dictionary<string, SemanticCaptureField>? record, string field, Dictionary<string, SemanticCaptureField>? enclosing = null)
        {
            if (field.Contains('.', StringComparison.Ordinal))
            {
                Unsupported ??= $"Capture path '{field}' requires traversal the reference evaluator does not support.";
                return null;
            }

            var present = record?.GetValueOrDefault(field) ?? enclosing?.GetValueOrDefault(field);
            if (present is null or { Kind: SemanticCaptureFieldKind.Value }) return present?.Value;
            Unsupported ??= $"Capture field '{field}' is a {present.Kind} value the reference evaluator cannot use as a scalar.";
            return null;
        }

        public IEnumerable<SemanticFact> Appends(
            ImmutableArray<SemanticCaptureAppend> appends,
            Dictionary<string, SemanticCaptureField>? before,
            Dictionary<string, SemanticCaptureField>? after,
            Dictionary<string, SemanticCaptureField>? enclosing)
        {
            if (Unsupported is not null) yield break;
            var item = after ?? before!;
            foreach (var append in appends.Where(append => Holds(append.When, before, after)))
            {
                if (Unsupported is not null) yield break;
                var eventContract = plan.Events[append.EventContract];
                var properties = eventContract.Properties.ToDictionary(property => property.Id);
                var values = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
                foreach (var mapping in append.Mappings)
                {
                    var property = properties[mapping.TargetProperty];
                    var value = mapping.Field is { } field
                        ? Read(item, field, enclosing) ?? SemanticValue.Null
                        : Value(mapping.Value!);
                    if (Unsupported is not null) yield break;
                    if (mapping.Field is not null) value = Coerce(value, property.Type);
                    _validator.Validate(value, property.Type, $"event property '{property.Name}'");
                    values.Add(new(property.Id, value));
                }

                _validator.Validate(key, append.EventSourceType, "capture event source");
                yield return new SemanticFact(append.EventContract, key, values.ToImmutable())
                {
                    Context = new(new(append.EventSourceType, key)),
                    Tags = eventContract.Tags.AddRange(append.Tags),
                    Occurred = occurrence?.Occurred
                };
            }
        }

        bool Holds(SemanticCaptureCondition? when, Dictionary<string, SemanticCaptureField>? before, Dictionary<string, SemanticCaptureField>? after) => when?.Kind switch
        {
            null => after is not null,
            SemanticCaptureConditionKind.Added => before is null && after is not null,
            SemanticCaptureConditionKind.Removed => before is not null && after is null,
            _ when after is null => false,
            SemanticCaptureConditionKind.AnyChanged => when.Fields.Any(field => Changed(before, after, field)),
            SemanticCaptureConditionKind.AllChanged => when.Fields.All(field => Changed(before, after, field)),
            SemanticCaptureConditionKind.Transition => before is not null &&
                SemanticValueRules.Text(Read(before, when.Fields[0])) == when.From &&
                SemanticValueRules.Text(Read(after, when.Fields[0])) == when.To,
            SemanticCaptureConditionKind.Expression => SemanticCaptureExpression.TryParse(when.Expression, out var expression) &&
                SemanticCaptureExpression.Evaluate(expression, field => Read(after, field)),
            _ => throw new InvalidSemanticContract($"Capture condition '{when.Kind}' is unknown.")
        };

        bool Changed(Dictionary<string, SemanticCaptureField>? before, Dictionary<string, SemanticCaptureField> after, string field)
        {
            var was = Read(before, field);
            var now = Read(after, field);
            return before is null
                ? after.ContainsKey(field)
                : before.ContainsKey(field) != after.ContainsKey(field) ||
                  (was is not null && now is not null && !SemanticValueRules.AreEqual(was, now));
        }

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
            if (primitive == SemanticPrimitiveType.DateTime && value is SemanticTextValue text)
            {
                if (!IsoInstant().IsMatch(text.Value) ||
                    !DateTimeOffset.TryParse(text.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant))
                {
                    throw new InvalidSemanticContract("A capture DateTime field must contain a complete ISO date and time with an explicit zone.");
                }

                return SemanticValue.Text(instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            }

            return value;
        }
    }
}
