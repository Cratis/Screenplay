// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { CommandSyntax } from '../Syntax/Commands';
import { EventSyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { EventSourceCatalog } from '../Syntax/EventSourceCatalog';
import { EventSourceResolutionKind } from '../Syntax/EventSources';
import { implicitDestination } from '../Syntax/ProductionDestinations';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { compatibleValue, uniqueByName } from './ResponseValidator';

interface Producer { event: EventSyntax; command: CommandSyntax | null; type: TypeRefSyntax | null }

export function validateSpecificationStreams(application: ApplicationSyntax, context: ParserContext): void {
    const resolver = new AuthoringProductionResolver(application);
    if (!resolver.slices.flatMap(entry => entry.slice.specifications).some(specification => [...specification.given, ...specification.thenEvents, ...(specification.whenAppended === null ? [] : [specification.whenAppended])].some(node => node.stream !== undefined || node.noStream !== undefined))) return;
    const catalog = new EventSourceCatalog(application);
    const concepts = uniqueByName(application.concepts);
    const composites = uniqueByName(application.types);
    const properties = new Map([...composites].map(([name, type]) => [name, uniqueByName(type.properties)]));
    const compatible = (value: Parameters<typeof compatibleValue>[0], type: TypeRefSyntax): boolean => compatibleValue(value, type, concepts, properties);
    const pathType = (command: CommandSyntax, path: string): TypeRefSyntax | null => {
        let fields = command.properties;
        let result: TypeRefSyntax | null = null;
        let optional = false;
        let collection = false;
        for (const segment of path.split('.')) {
            const matches = fields.filter(field => field.name === segment);
            if (matches.length !== 1) return null;
            const type = matches[0].type;
            optional ||= type.isOptional;
            collection ||= type.isCollection;
            result = { ...type, isOptional: optional, isCollection: collection };
            fields = composites.get(type.name)?.properties ?? [];
        }
        return result;
    };
    const eventOf = (reference: string, slice: typeof resolver.slices[number]['slice']): EventSyntax | null => {
        const declared = resolver.resolve(reference, slice).declaration;
        return declared?.node.kind === 'EventSyntax' ? declared.node : null;
    };
    const producers: Producer[] = [];
    for (const { slice } of resolver.slices) {
        for (const command of slice.commands) for (const produced of command.produces) {
            if (!resolver.isEventProduction(produced, slice)) continue;
            const event = eventOf(produced.event, slice);
            if (event === null) continue;
            const destination = produced.for?.kind === 'PathExpressionSyntax' ? produced.for.path : implicitDestination(command, produced, { resolver, slice });
            const type = destination === 'new event source'
                ? { kind: 'TypeRefSyntax' as const, name: 'Uuid', isOptional: false, isCollection: false, location: produced.location }
                : destination === undefined ? null : pathType(command, destination);
            producers.push({ event, command, type });
        }
        for (const produced of slice.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.produces)) {
            const event = eventOf(produced.event, slice);
            if (event !== null) producers.push({ event, command: null, type: null });
        }
        for (const capture of slice.captures) for (const appended of [...capture.appends, ...capture.children.flatMap(child => child.appends), ...capture.nested.flatMap(nested => nested.appends)]) {
            const event = eventOf(appended.event, slice);
            if (event !== null) producers.push({ event, command: null, type: null });
        }
    }
    for (const { slice, scope } of resolver.slices) for (const specification of slice.specifications) {
        const commands = resolver.slices.flatMap(entry => entry.slice.commands.map(command => ({ command, scope: entry.scope })));
        const candidates = specification.when === null ? [] : commands.filter(entry => entry.command.name === specification.when!.commandType);
        let command: CommandSyntax | null = null;
        for (let depth = scope.length; depth >= 0 && command === null; depth--) {
            const visible = candidates.filter(entry => scope.slice(0, depth).every((segment, index) => entry.scope[index] === segment));
            if (visible.length > 0) { command = visible.length === 1 ? visible[0].command : null; break; }
        }
        const occurrences = [...specification.given.map(node => ({ node, required: true, expected: false })),
            ...(specification.whenAppended === null ? [] : [{ node: specification.whenAppended, required: true, expected: false }]),
            ...specification.thenEvents.map(node => ({ node, required: false, expected: true }))];
        for (const { node, required, expected } of occurrences) {
            if (node.stream === undefined && node.noStream === undefined) continue;
            const event = eventOf(node.eventType, slice);
            const eventProducers = producers.filter(producer => producer.event === event);
            if (node.stream !== undefined) {
                const route = node.stream;
                const resolution = catalog.resolve(route.eventSource, route.stream);
                if (resolution.kind !== EventSourceResolutionKind.Unique) {
                    const state = { ambiguous: 'Ambiguous', notFound: 'NotFound', wrongKind: 'WrongKind' }[resolution.kind];
                    context.error(DiagnosticCodes.InvalidSpecificationStreamRoute, `Stream '${route.eventSource}.${route.stream}' is ${state}; routing requires one physical source and stream.`, route.location);
                    continue;
                }
                const source = resolution.sources[0];
                const stream = resolution.streams[0];
                if ((stream.streamId === null) !== (route.streamId === null))
                    context.error(DiagnosticCodes.InvalidSpecificationStreamRoute, stream.streamId === null ? 'An unkeyed stream cannot take a streamId mapping.' : 'This keyed stream requires a streamId mapping.', route.location);
                if (route.streamId !== null) {
                    const value = route.streamId.source;
                    if (value.kind !== 'LiteralExpressionSyntax' || value.value === null || value.value === '' || stream.streamId !== null && !compatible(value, stream.streamId))
                        context.error(DiagnosticCodes.InvalidSpecificationStreamRoute, "A specification stream id needs a nonempty concrete scalar literal compatible with the stream's declared type.", value.location);
                }
                let identifier = source.identifier;
                if (identifier === null) {
                    const types = eventProducers.map(producer => producer.type);
                    const distinct = new Set(types.filter(type => type !== null).map(type => `${type.name}:${type.isOptional}:${type.isCollection}`));
                    if (types.length === 0 || types.some(type => type === null) || distinct.size !== 1)
                        context.error(DiagnosticCodes.InvalidSpecificationStreamEventSource, `Declare an identifier on source '${source.name}'; the event's producers do not supply one unambiguous destination type.`, route.location);
                    else identifier = types[0];
                }
                if (required && node.for === null)
                    context.error(DiagnosticCodes.InvalidSpecificationStreamEventSource, "A routed given or when append event requires 'for <literal>'.", route.location);
                else if (node.for !== null && (node.for.kind !== 'LiteralExpressionSyntax' || node.for.value === null || identifier !== null && (identifier.isCollection || identifier.isOptional || !compatible(node.for, identifier))))
                    context.error(DiagnosticCodes.InvalidSpecificationStreamEventSource, "A routed event's for value must be a concrete literal compatible with the source's identifier type.", node.for.location);
            }
            if (!expected || command === null || eventProducers.length === 0 || eventProducers.some(producer => producer.command !== command)) continue;
            const commandRoute = command.stream?.propertyCandidate === null ? command.stream : null;
            let contradicts = node.noStream !== undefined ? commandRoute !== null : commandRoute === null || commandRoute.eventSource !== node.stream!.eventSource || commandRoute.stream !== node.stream!.stream;
            if (node.for?.kind === 'LiteralExpressionSyntax' && eventProducers.every(producer => producer.type !== null && !compatible(node.for!, producer.type))) contradicts = true;
            if (contradicts) context.error(DiagnosticCodes.SpecificationStreamContradictsCommand, 'The expected event route contradicts its only producer, the command under test.', (node.stream ?? node.noStream)!.location);
        }
    }
}
