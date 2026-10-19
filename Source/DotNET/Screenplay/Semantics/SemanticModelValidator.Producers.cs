// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static SemanticTypeReference DeclaredEventSourceType(IEnumerable<SemanticCommand> commands, SemanticId eventContract) =>
        DeclaredEventSourceType(commands, [], [], eventContract);

    // Since ESM v6 a reaction or a capture can append an event too: a reaction to the event source its 'for' names, or
    // to that of the event that set it off, and a capture to the one its record key names.
    internal static SemanticTypeReference DeclaredEventSourceType(
        IEnumerable<SemanticCommand> commands,
        IEnumerable<SemanticReaction> reactions,
        IEnumerable<SemanticCapture> captures,
        SemanticId eventContract)
    {
        var sources = new ProducerSources([.. commands], [.. reactions], [.. captures]);
        var producerTypes = ProducerTypes(sources, eventContract, [])
            .Distinct().ToArray();
        if (producerTypes.Length != 1)
        {
            throw new InvalidSemanticContract("A specification event source needs one unambiguous declared producer destination type.");
        }

        return producerTypes[0];
    }

    internal static SemanticTypeReference? ProducedEventSourceType(SemanticCommand command, SemanticProducedEvent produced) =>
        produced.Destination is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } route &&
        command.Properties.SingleOrDefault(property => property.Id == route.Target) is { } destination &&
        (produced.Route is not null || !destination.IsIdentifier || command.Properties.Any(property => property.IsGenerated && property.IsIdentifier))
            ? destination.Type
            : command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type;

    static List<SemanticTypeReference> ProducerTypes(ProducerSources sources, SemanticId eventContract, HashSet<SemanticId> visited)
    {
        var types = CommandProducerTypes(sources.Commands, eventContract);
        visited.Add(eventContract);
        foreach (var trigger in sources.Reactions.SelectMany(reaction => reaction.Triggers))
        {
            foreach (var produced in trigger.Produces.Concat(trigger.Invokes.SelectMany(invocation => invocation.OnRefused).SelectMany(branch => branch.Produces)).Where(produced => produced.EventContract == eventContract))
            {
                types.AddRange(ReactionProducerTypes(sources, trigger, produced, visited));
            }
        }

        types.AddRange(sources.Captures.SelectMany(CaptureAppends)
            .Where(append => append.EventContract == eventContract)
            .Select(append => append.EventSourceType));
        return types;
    }

    static List<SemanticTypeReference> CommandProducerTypes(SemanticCommand[] commands, SemanticId eventContract) =>
        [.. commands.SelectMany(command => command.Produces
            .Where(produced => produced.EventContract == eventContract)
            .Select(produced => ProducedEventSourceType(command, produced)))
            .OfType<SemanticTypeReference>()];

    static List<SemanticTypeReference> ReactionProducerTypes(
        ProducerSources sources,
        SemanticReactionTrigger trigger,
        SemanticProducedEvent produced,
        HashSet<SemanticId> visited)
    {
        if (produced.DestinationType is { } type)
        {
            return [type];
        }

        return trigger.Kind == SemanticReactionTriggerKind.Event && !visited.Contains(trigger.Source)
            ? ProducerTypes(sources, trigger.Source, visited)
            : [];
    }

    static IEnumerable<SemanticCaptureAppend> CaptureAppends(SemanticCapture capture) =>
        capture.Appends
            .Concat(capture.Children.SelectMany(children => children.Appends))
            .Concat(capture.Nested.SelectMany(nested => nested.Appends));

    sealed record ProducerSources(SemanticCommand[] Commands, SemanticReaction[] Reactions, SemanticCapture[] Captures);
}
