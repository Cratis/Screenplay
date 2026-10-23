// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static IEnumerable<CaptureAppendSyntax> CaptureAppends(CaptureSyntax capture) =>
            capture.Appends.Concat(capture.Children.SelectMany(child => child.Appends)).Concat(capture.Nested.SelectMany(nested => nested.Appends));

        // Commands retain their established identity. Otherwise every capture must agree on a scalar event key.
        SemanticTypeReference CaptureEventSourceType(string eventName, HashSet<string>? visited = null)
        {
            var types = CommandEventSourceTypes(eventName);
            if (AllSlices().SelectMany(entry => entry.Slice.Commands).Any(command => command.Produces.Any(produced => ShortName(produced.Event) == eventName)))
                return types.Length == 1 ? types[0] : SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
            var captures = AllSlices().SelectMany(entry => entry.Slice.Captures).Where(capture => CaptureAppends(capture).Any(append => ShortName(append.Event) == eventName)).ToArray();
            var keys = captures.Select(CaptureSourceKeyType).ToArray();
            var distinct = keys.OfType<SemanticTypeReference>().Distinct().ToArray();
            if (keys.Length == 0 || keys.Any(type => type is null) || distinct.Length != 1)
                return SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
            var producers = ProducerEventSourceTypes(eventName, visited ?? [], includeCaptures: false);
            return producers.All(type => type == distinct[0]) ? distinct[0] : SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        }

        SemanticTypeReference? CaptureSourceKeyType(CaptureSyntax capture)
        {
            if (capture.Source is not { } source || !CaptureEventsSource.IsEvents(source)) return null;
            var keys = new List<SemanticTypeReference>();
            foreach (var setting in CaptureEventsSource.Events(source))
            {
                if (!_events.TryGetValue(ShortName(setting.Value), out var @event)) return null;
                var properties = @event.Contract.Properties.Where(property => property.Name == capture.Key).ToArray();
                if (properties.Length != 1) return null;
                var type = properties[0].Type;
                if (type.IsCollection || type.IsOptional || type.Kind == SemanticTypeReferenceKind.CompositeType) return null;
                keys.Add(type);
            }

            var types = keys.Distinct().ToArray();
            return types.Length == 1 ? types[0] : null;
        }

        SemanticTypeReference[] CommandEventSourceTypes(string eventName) =>
            [.. AllSlices().SelectMany(entry => entry.Slice.Commands)
                .SelectMany(command => command.Produces.Where(produced => ShortName(produced.Event) == eventName)
                    .Select(produced => command.Properties.Any(property => property.IsGenerated && property.IsIdentifier) && produced.For is PathExpressionSyntax route
                        ? command.Properties.SingleOrDefault(property => property.Name == route.Path)
                        : command.Properties.SingleOrDefault(property => property.IsIdentifier)))
                .Where(property => property is not null)
                .Select(property => BindTypeReference(property!.Type))
                .Distinct()];

        // The event-source types every producer of an event gives it - commands, reactions and captures - read from
        // the syntax, so a specification can type 'for' on an event whichever slice produces it.
        SemanticTypeReference[] ProducerEventSourceTypes(string eventName, HashSet<string> visited, bool includeCaptures = true)
        {
            visited.Add(eventName);
            var types = CommandEventSourceTypes(eventName).ToList();
            foreach (var reaction in AllSlices().SelectMany(entry => entry.Slice.Reactions))
            {
                foreach (var trigger in reaction.Triggers)
                {
                    foreach (var produced in (trigger.Produces ?? []).Concat((trigger.Invokes ?? []).SelectMany(invocation => invocation.OnRefused).SelectMany(branch => branch.Produces)).Where(produced => ShortName(produced.Event) == eventName))
                    {
                        var named = (trigger.Source as NamedTriggerSourceSyntax)?.Name;
                        switch (produced.For)
                        {
                            case PathExpressionSyntax path when named is not null && _events.TryGetValue(ShortName(named), out var source) &&
                                source.Properties.TryGetValue(path.Path, out var property):
                                types.Add(property.Type);
                                break;
                            case PathExpressionSyntax path when named is not null && _triggerDeclarations.TryGetValue(named, out var declared) &&
                                declared.Properties.TryGetValue(path.Path, out var property):
                                types.Add(property.Type);
                                break;
                            case LiteralExpressionSyntax:
                                types.Add(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
                                break;
                            case null when named is not null && _events.ContainsKey(ShortName(named)) && !visited.Contains(ShortName(named)):
                                types.AddRange(ProducerEventSourceTypes(ShortName(named), visited));
                                break;
                        }
                    }
                }
            }

            var appended = AllSlices().SelectMany(entry => entry.Slice.Captures)
                .SelectMany(capture => capture.Appends
                    .Concat(capture.Children.SelectMany(children => children.Appends))
                    .Concat(capture.Nested.SelectMany(nested => nested.Appends)))
                .Any(append => ShortName(append.Event) == eventName);
            if (includeCaptures && appended) types.Add(CaptureEventSourceType(eventName, visited));
            return [.. types.Distinct()];
        }
    }
}
