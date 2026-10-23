// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static ImmutableArray<SemanticCaptureTranslation> Translations(CaptureMapEntrySyntax entry) =>
            [.. entry.Translations.Select(translation => new SemanticCaptureTranslation(translation.From, translation.To))];

        static ImmutableArray<SemanticCaptureTemplatePart>? TemplateParts(TemplateExpressionSyntax template)
        {
            var parts = ImmutableArray.CreateBuilder<SemanticCaptureTemplatePart>();
            foreach (var part in template.Parts)
            {
                switch (part)
                {
                    case TemplateTextSyntax text:
                        parts.Add(new(text.Text, null));
                        break;
                    case TemplateInterpolationSyntax { Expression: PathExpressionSyntax path }:
                        parts.Add(new(null, path.Path));
                        break;
                    case TemplateInterpolationSyntax { Expression: SourceItemExpressionSyntax item }:
                        parts.Add(new(null, item.Path));
                        break;
                    default:
                        return null;
                }
            }

            return parts.Count == 0 ? null : parts.ToImmutable();
        }

        SemanticCapture? BindCapture(SemanticAddress slice, CaptureSyntax capture)
        {
            UsesV6 = true;
            SemanticCaptureEventsSource? eventsSource = null;
            if (capture.Source is { } source && CaptureEventsSource.IsEvents(source))
            {
                UsesPublicEvents = true;
                eventsSource = BindEventsSource(capture, source);
            }
            else if (capture.Source is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Capture '{capture.Name}' source is realization metadata; the reference evaluator is handed records, never a source.", capture.Source.Location);
            }

            var id = Resolve(SemanticAddress.ForCapture(slice, capture.Name), capture.Location);
            if (string.IsNullOrWhiteSpace(capture.Key))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture.Name}' needs a 'key' to say which instance a record is, and so which event source it appends to.", capture.Location);
                return null;
            }

            var children = capture.Children.Select(value => new SemanticCaptureChildren(
                value.Property,
                value.IdentifiedBy,
                BindCaptureMap(capture.Name, value.Map),
                BindCaptureAppends(capture.Name, value.Appends)));
            var nested = capture.Nested.Select(value => new SemanticCaptureNested(
                value.Property,
                BindCaptureMap(capture.Name, value.Map),
                BindCaptureAppends(capture.Name, value.Appends)));
            return new(id, capture.Name, capture.Key, BindCaptureMap(capture.Name, capture.Map), BindCaptureAppends(capture.Name, capture.Appends))
            {
                Children = [.. children],
                Nested = [.. nested],
                EventsSource = eventsSource
            };
        }

        SemanticCaptureEventsSource? BindEventsSource(CaptureSyntax capture, CaptureSourceSyntax source)
        {
            var events = ImmutableArray.CreateBuilder<SemanticId>();
            foreach (var setting in CaptureEventsSource.Events(source))
            {
                var name = setting.Value;
                if (!_events.TryGetValue(ShortName(name), out var @event))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Capture '{capture.Name}' reads '{name}', which is not a declared event. Declare the foreign public event with its own fields as 'event <Name> from \"<store>\"'.", setting.Location);
                    continue;
                }

                events.Add(@event.Contract.Id);
            }

            return events.Count == 0 ? null : new(events.ToImmutable()) { KeyIsEventSource = capture.KeyIsEventSource };
        }

        ImmutableArray<SemanticCaptureMap> BindCaptureMap(string capture, IEnumerable<CaptureMapOperationSyntax> operations)
        {
            var map = ImmutableArray.CreateBuilder<SemanticCaptureMap>();
            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case CaptureMapEntrySyntax { Source: PathExpressionSyntax path } entry:
                        map.Add(new(SemanticCaptureMapKind.Value, [entry.Property]) { Source = path.Path, Translations = Translations(entry) });
                        break;
                    case CaptureMapEntrySyntax { Source: TemplateExpressionSyntax template } entry when TemplateParts(template) is { } parts:
                        map.Add(new(SemanticCaptureMapKind.Template, [entry.Property]) { Template = parts, Translations = Translations(entry) });
                        break;
                    case CaptureSplitSyntax { Source: PathExpressionSyntax path } split:
                        map.Add(new(SemanticCaptureMapKind.Split, [.. split.Targets]) { Source = path.Path, Separator = split.Separator });
                        break;
                    default:
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture}' maps from something other than a source field or a template of source fields.", operation.Location);
                        break;
                }
            }

            return map.ToImmutable();
        }

        ImmutableArray<SemanticCaptureAppend> BindCaptureAppends(string capture, IEnumerable<CaptureAppendSyntax> appends)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticCaptureAppend>();
            foreach (var append in appends)
            {
                if (!_events.TryGetValue(ShortName(append.Event), out var @event))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Capture '{capture}' appends '{append.Event}', which is not a declared event.", append.Location);
                    continue;
                }

                var when = append.When is null ? null : BindCaptureCondition(capture, append.When);
                if (append.When is not null && when is null)
                {
                    continue;
                }

                var mappings = ImmutableArray.CreateBuilder<SemanticCaptureMapping>();
                foreach (var mapping in append.Mappings)
                {
                    if (!@event.Properties.TryGetValue(mapping.Property, out var target))
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Capture '{capture}' sets '{mapping.Property}', which '{@event.Syntax.Name}' does not declare.", mapping.Location);
                        continue;
                    }

                    switch (mapping.Source)
                    {
                        case SourceItemExpressionSyntax item:
                            mappings.Add(new(target.Id) { Field = item.Path });
                            break;
                        case ContextExpressionSyntax { Path: "occurred" } context when BindOccurrence(context, target.Type) is { } occurred:
                            mappings.Add(new(target.Id) { Value = occurred });
                            break;
                        case LiteralExpressionSyntax literal when BindConcreteValue(literal, target.Type, "capture mapping", false) is { } value:
                            mappings.Add(new(target.Id) { Value = SemanticExpression.FromValue(value) });
                            break;
                        case PathExpressionSyntax path when EnumerationMember(path.Path, target.Type) is { } member:
                            mappings.Add(new(target.Id) { Value = SemanticExpression.FromValue(SemanticValue.Text(member)) });
                            break;
                        case LiteralExpressionSyntax:
                            break;
                        default:
                            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture}' sets '{mapping.Property}' from something other than a source field ($.field), a value or $context.occurred.", mapping.Source.Location);
                            break;
                    }
                }

                bound.Add(new(@event.Contract.Id, CaptureEventSourceType(@event.Syntax.Name), when, mappings.ToImmutable()) { Tags = BindTags(append.Tags) });
            }

            return bound.ToImmutable();
        }

        SemanticCaptureCondition? BindCaptureCondition(string capture, CaptureWhenSyntax when)
        {
            var fields = when.Properties.ToImmutableArray();
            switch (when.Kind)
            {
                case CaptureWhenKind.PropertyChanged or CaptureWhenKind.LogicalOr:
                    return new(SemanticCaptureConditionKind.AnyChanged, fields);
                case CaptureWhenKind.LogicalAnd:
                    return new(SemanticCaptureConditionKind.AllChanged, fields);
                case CaptureWhenKind.ValueTransition:
                    return new(SemanticCaptureConditionKind.Transition, fields) { From = when.FromValue, To = when.ToValue };
                case CaptureWhenKind.Added:
                    return new(SemanticCaptureConditionKind.Added, []);
                case CaptureWhenKind.Removed:
                    return new(SemanticCaptureConditionKind.Removed, []);
                case CaptureWhenKind.Expression when SemanticCaptureExpression.TryParse(when.Expression, out _):
                    return new(SemanticCaptureConditionKind.Expression, []) { Expression = when.Expression };
                case CaptureWhenKind.Expression:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture}' condition {when.Expression} is outside the portable template grammar: fields, literals, comparisons, &&, ||, ! and parentheses.", when.Location);
                    return null;
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture}' condition '{when.Kind}' has no portable meaning.", when.Location);
                    return null;
            }
        }
    }
}
