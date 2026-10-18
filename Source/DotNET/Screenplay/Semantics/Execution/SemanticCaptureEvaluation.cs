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
/// records. A first observation is added only, at root, child and nested level. With previous state, a field the
/// earlier record lacked has changed when the new one has it. <c>$.field</c> reads the item being evaluated, and the
/// enclosing record when the item lacks it.
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
    /// Evaluates reached capture appends and submits each fact before evaluating the next effect.
    /// </summary>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="capture">The capture.</param>
    /// <param name="previous">The record the capture last saw for the key, if any.</param>
    /// <param name="current">The record presented now.</param>
    /// <param name="occurrence">The occurrence, when the scenario states a clock.</param>
    /// <param name="accept">Accepts one append and settles its reactions; returns the first terminal failure.</param>
    /// <returns>The first terminal disposition, or an empty result when every reached effect succeeded.</returns>
    /// <exception cref="InvalidSemanticContract">A value does not fit the event it is appended to.</exception>
    public static SemanticCaptureEvaluationResult Evaluate(
        SemanticExecutionPlan plan,
        SemanticCapture capture,
        SemanticCaptureRecord? previous,
        SemanticCaptureRecord current,
        SemanticCommandOccurrence? occurrence,
        Func<SemanticFact, SemanticExecutionResult?> accept)
    {
        var key = current.Fields.SingleOrDefault(field => field.Name == capture.Key && field.Kind == SemanticCaptureFieldKind.Value)?.Value
            ?? throw new InvalidSemanticContract($"The record presented to capture '{capture.Name}' has no key '{capture.Key}'.");
        var context = new Context(plan, capture, key, occurrence, accept);

        ValidatePreconditions(context, capture, previous, current);
        return EvaluateScopes(context, capture, previous, current);
    }

    // Structural and identity preconditions reject the whole record before any append. Value maps and
    // guards are not probed here: an unsupported later effect cannot replace an earlier disposition.
    static void ValidatePreconditions(Context context, SemanticCapture capture, SemanticCaptureRecord? previous, SemanticCaptureRecord current)
    {
        foreach (var children in capture.Children)
        {
            Index(Records(previous, children.Field), children.IdentifiedBy);
            Index(Records(current, children.Field), children.IdentifiedBy);
        }

        foreach (var nested in capture.Nested)
        {
            Nested(previous, nested.Field);
            Nested(current, nested.Field);
        }

        context.ValidateSource(capture.Appends);
        foreach (var children in capture.Children) context.ValidateSource(children.Appends);
        foreach (var nested in capture.Nested) context.ValidateSource(nested.Appends);
    }

    static SemanticCaptureEvaluationResult EvaluateScopes(Context context, SemanticCapture capture, SemanticCaptureRecord? previous, SemanticCaptureRecord current)
    {
        var before = previous is null ? null : Map(context, Fields(previous), capture.Map);
        if (context.Stopped) return context.Result;
        var after = Map(context, Fields(current), capture.Map);
        if (context.Stopped) return context.Result;
        context.Appends(capture.Appends, before, after, null);
        if (context.Stopped) return context.Result;
        foreach (var children in capture.Children)
        {
            var was = Records(previous, children.Field);
            var now = Records(current, children.Field);
            var wasById = Index(was, children.IdentifiedBy);
            var nowById = Index(now, children.IdentifiedBy);
            foreach (var child in now)
            {
                var earlier = wasById.GetValueOrDefault(Identity(child, children.IdentifiedBy));
                context.RecordAppends(children.Appends, earlier, child, children.Map, after);
                if (context.Stopped) return context.Result;
            }

            foreach (var removed in was.Where(record => !nowById.ContainsKey(Identity(record, children.IdentifiedBy))))
            {
                context.RecordAppends(children.Appends, removed, null, children.Map, after);
                if (context.Stopped) return context.Result;
            }
        }

        foreach (var nested in capture.Nested)
        {
            var was = Nested(previous, nested.Field);
            var now = Nested(current, nested.Field);
            if (was is null && now is null) continue;
            context.RecordAppends(nested.Appends, was, now, nested.Map, after);
            if (context.Stopped) return context.Result;
        }

        return context.Result;
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.NonBacktracking)]
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
                    var source = context.Read(values, operation.Source!);
                    if (source.Unsupported is not null) return values;
                    values[operation.Targets[0]] = Scalar(operation.Targets[0], Translate(source.Value ?? SemanticValue.Null, operation.Translations));
                    break;
                case SemanticCaptureMapKind.Template:
                    var partsOfText = new List<string>();
                    foreach (var part in operation.Template)
                    {
                        if (part.Text is { } literal)
                        {
                            partsOfText.Add(literal);
                        }
                        else
                        {
                            var field = context.Read(values, part.Field!);
                            if (field.Unsupported is not null) return values;
                            partsOfText.Add(SemanticValueRules.CaptureText(field.Value));
                        }
                    }

                    values[operation.Targets[0]] = Scalar(operation.Targets[0], Translate(SemanticValue.Text(string.Concat(partsOfText)), operation.Translations));
                    break;
                default:
                    var splitSource = context.Read(values, operation.Source!);
                    if (splitSource.Unsupported is not null) return values;
                    var parts = SemanticValueRules.CaptureText(splitSource.Value)
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
        translations.FirstOrDefault(translation => translation.From == SemanticValueRules.CaptureText(value)) is { } match
            ? SemanticValue.Text(match.To)
            : value;

    sealed class Context(SemanticExecutionPlan plan, SemanticCapture capture, SemanticValue key, SemanticCommandOccurrence? occurrence, Func<SemanticFact, SemanticExecutionResult?> accept, bool eventsSource = false)
    {
        readonly SemanticValueValidator _validator = new(
            plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
            plan.Model.Application.Types.ToDictionary(type => type.Id));

        readonly Dictionary<SemanticId, string> _propertyNames = plan.Model.Application.Types.SelectMany(type => type.Properties).ToDictionary(property => property.Id, property => property.Name);

        public string? Unsupported { get; private set; }

        public SemanticExecutionResult? Failure { get; private set; }

        public bool Stopped => Unsupported is not null || Failure is not null;

        public SemanticCaptureEvaluationResult Result => new(Unsupported, Failure);

        public void ValidateSource(ImmutableArray<SemanticCaptureAppend> appends)
        {
            foreach (var append in appends)
            {
                var type = append.EventSourceType;
                var primitive = type.Kind == SemanticTypeReferenceKind.Concept
                    ? plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Primitive
                    : type.Primitive;
                if (key is SemanticNumberValue && primitive == SemanticPrimitiveType.Text)
                {
                    var value = SemanticValueRules.CaptureText(key);
                    var eventName = plan.Events[append.EventContract].Name;
                    throw new InvalidSemanticContract($"Capture '{capture.Name}' key '{capture.Key}' is {value} (number); event '{eventName}' has a text event source, so the key must be text: write \"{value}\".");
                }

                _validator.Validate(key, type, "capture event source");
            }
        }

        public void RecordAppends(
            ImmutableArray<SemanticCaptureAppend> appends,
            SemanticCaptureRecord? previous,
            SemanticCaptureRecord? current,
            ImmutableArray<SemanticCaptureMap> map,
            Dictionary<string, SemanticCaptureField> enclosing)
        {
            var before = previous is null ? null : Map(this, Fields(previous), map);
            if (Stopped) return;
            var after = current is null ? null : Map(this, Fields(current), map);
            if (Stopped) return;
            Appends(appends, before, after, enclosing);
        }

        public SemanticCaptureExpression.Lookup Read(Dictionary<string, SemanticCaptureField>? record, string field, Dictionary<string, SemanticCaptureField>? enclosing = null, bool structured = false)
        {
            if (field.Contains('.', StringComparison.Ordinal))
            {
                Unsupported ??= $"Capture path '{field}' requires traversal the reference evaluator does not support.";
                return new(null, Unsupported);
            }

            var present = record?.GetValueOrDefault(field) ?? enclosing?.GetValueOrDefault(field);

            // A list or object read from an event keeps its whole value, so it can be copied into a list or object property.
            if (structured && present is { Value: SemanticCompositeValue or SemanticArrayValue }) return new(present.Value, null);
            if (present is null or { Kind: SemanticCaptureFieldKind.Value, Value: not (SemanticCompositeValue or SemanticArrayValue) }) return new(present?.Value, null);
            Unsupported ??= $"Capture field '{field}' is a {present.Kind} value the reference evaluator cannot use as a scalar.";
            return new(null, Unsupported);
        }

        public void Appends(
            ImmutableArray<SemanticCaptureAppend> appends,
            Dictionary<string, SemanticCaptureField>? before,
            Dictionary<string, SemanticCaptureField>? after,
            Dictionary<string, SemanticCaptureField>? enclosing)
        {
            if (Stopped) return;
            var item = after ?? before!;
            foreach (var append in appends)
            {
                // A reached unsupported guard ends evaluation, even when it does not hold. Filtering first would
                // evaluate a later guard and could mask unsupported as an ordinary contract rejection.
                var holds = Holds(append.When, before, after);
                if (Stopped) return;
                if (!holds) continue;
                var eventContract = plan.Events[append.EventContract];
                var properties = eventContract.Properties.ToDictionary(property => property.Id);
                var values = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
                foreach (var mapping in append.Mappings)
                {
                    var property = properties[mapping.TargetProperty];
                    var structured = property.Type.IsCollection || property.Type.Kind == SemanticTypeReferenceKind.CompositeType;
                    var source = mapping.Field is { } field
                        ? Read(item, field, enclosing, structured)
                        : new SemanticCaptureExpression.Lookup(Value(mapping.Value!), null);
                    if (source.Unsupported is not null) return;
                    var value = source.Value ?? SemanticValue.Null;
                    if (mapping.Field is not null) value = structured ? Retarget(value, property.Type, property.Name) : Coerce(value, property.Type);
                    _validator.Validate(value, property.Type, $"event property '{property.Name}'");
                    values.Add(new(property.Id, value));
                }

                _validator.Validate(key, append.EventSourceType, "capture event source");
                Failure = accept(new SemanticFact(append.EventContract, key, values.ToImmutable())
                {
                    Context = new(new(append.EventSourceType, key)),
                    Tags = eventContract.Tags.AddRange(append.Tags),
                    Occurred = occurrence?.Occurred
                });
                if (Stopped) return;
            }
        }

        bool Holds(SemanticCaptureCondition? when, Dictionary<string, SemanticCaptureField>? before, Dictionary<string, SemanticCaptureField>? after) => when?.Kind switch
        {
            null => after is not null,
            SemanticCaptureConditionKind.Added => before is null && after is not null,
            SemanticCaptureConditionKind.Removed => before is not null && after is null,
            _ when after is null => false,
            _ when before is null && when is { Kind: SemanticCaptureConditionKind.AnyChanged or SemanticCaptureConditionKind.AllChanged } first => FirstObservationDoesNotModify(first, after),
            SemanticCaptureConditionKind.AnyChanged => when.Fields.Any(field => Unsupported is null && Changed(before!, after, field)),
            SemanticCaptureConditionKind.AllChanged => when.Fields.All(field => Unsupported is null && Changed(before!, after, field)),
            SemanticCaptureConditionKind.Transition => before is not null
                ? Transition(before, after, when)
                : eventsSource && FirstObservationDoesNotModify(when, after),
            SemanticCaptureConditionKind.Expression => ExpressionHolds(when.Expression, after),
            _ => throw new InvalidSemanticContract($"Capture condition '{when.Kind}' is unknown.")
        };

        bool FirstObservationDoesNotModify(SemanticCaptureCondition when, Dictionary<string, SemanticCaptureField> after)
        {
            // A first observation cannot modify a field, but watching a non-scalar still fails closed.
            foreach (var field in when.Fields)
            {
                if (Read(after, field).Unsupported is not null) return false;
            }
            return false;
        }

        bool ExpressionHolds(string? text, Dictionary<string, SemanticCaptureField> after)
        {
            if (!SemanticCaptureExpression.TryParse(text, out var expression)) return false;
            var evaluated = SemanticCaptureExpression.Evaluate(expression, field => Read(after, field));
            Unsupported ??= evaluated.Unsupported;
            return evaluated.Holds;
        }

        bool Transition(Dictionary<string, SemanticCaptureField> before, Dictionary<string, SemanticCaptureField> after, SemanticCaptureCondition when)
        {
            var was = Read(before, when.Fields[0]);
            if (was.Unsupported is not null || SemanticValueRules.CaptureText(was.Value) != when.From) return false;
            var now = Read(after, when.Fields[0]);
            return now.Unsupported is null && SemanticValueRules.CaptureText(now.Value) == when.To;
        }

        bool Changed(Dictionary<string, SemanticCaptureField> before, Dictionary<string, SemanticCaptureField> after, string field)
        {
            var was = Read(before, field);
            if (was.Unsupported is not null) return false;
            var now = Read(after, field);
            if (now.Unsupported is not null) return false;
            return before.ContainsKey(field) != after.ContainsKey(field) ||
                (was.Value is not null && now.Value is not null && !SemanticValueRules.AreEqual(was.Value, now.Value));
        }

        SemanticValue Value(SemanticExpression expression) => expression switch
        {
            SemanticValueExpression literal => literal.Value,
            SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } when occurrence is not null =>
                SemanticValue.Text(occurrence.Occurred.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)),
            SemanticEventContextExpression => throw new InvalidSemanticContract("A capture using $context.occurred needs the scenario's 'given clock'."),
            _ => throw new InvalidSemanticContract("A capture mapping expression is unknown.")
        };

        // A list or object copied from an event arrives typed by the public event's own composite type. It is written again
        // in the target's type by property name, the way Chronicle reads the same JSON shape into either type.
        SemanticValue Retarget(SemanticValue value, SemanticTypeReference target, string where)
        {
            if (value is SemanticNullValue) return value;
            if (target.IsCollection)
            {
                return value is SemanticArrayValue array
                    ? new SemanticArrayValue([.. array.Values.Select(element => Retarget(element, target with { IsCollection = false, IsOptional = false }, where))])
                    : throw new InvalidSemanticContract($"Event property '{where}' is a list, but the captured value is not.");
            }

            if (target.Kind != SemanticTypeReferenceKind.CompositeType)
            {
                return Coerce(value, target);
            }

            if (value is not SemanticCompositeValue composite)
            {
                throw new InvalidSemanticContract($"Event property '{where}' is an object, but the captured value is not.");
            }

            var type = plan.Model.Application.Types.Single(candidate => candidate.Id == target.Target);
            var byName = type.Properties.ToDictionary(property => property.Name, StringComparer.Ordinal);
            var written = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            foreach (var entry in composite.Properties)
            {
                var name = _propertyNames.GetValueOrDefault(entry.TargetProperty) ?? throw new InvalidSemanticContract($"Event property '{where}' holds an unknown captured property.");
                if (!byName.TryGetValue(name, out var property))
                {
                    throw new InvalidSemanticContract($"Event property '{where}' has no property '{name}' in '{type.Name}', so the captured value cannot be copied.");
                }

                written.Add(new(property.Id, Retarget(entry.Value, property.Type, $"{where}.{name}")));
            }

            return new SemanticCompositeValue(written.ToImmutable());
        }

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
