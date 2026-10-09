// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { CommandSyntax } from '../Syntax/Commands';
import { EventSyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { EventSourceCatalog } from '../Syntax/EventSourceCatalog';
import { EventSourceResolutionKind } from '../Syntax/EventSources';
import { formatSpecificationStreamId } from './SpecificationRouteComparison';
import { streamIdFailureMessage } from '../Syntax/StreamIdFormatter';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { commandDestinationType } from './CommandDestinationTypes';
import { formatStreamIdLiteral } from './EventSourceValidator';
import { ApplicationSyntax } from '../Syntax/Structure';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { validateStreamIdParts } from './CompositeStreamIdValidator';
import { ParserContext } from './ParserContext';
import { compatibleValue, uniqueByName } from './ResponseValidator';
import { expandEffectiveSpecificationExamples, specificationExamples } from './SpecificationCommandExamples';
import { SpecificationEventSyntax } from '../Syntax/Specifications';

interface Producer { event: EventSyntax; command: CommandSyntax | null; type: TypeRefSyntax | null }

export function validateSpecificationStreams(application: ApplicationSyntax, context: ParserContext): void {
    const examples = specificationExamples(application).resolvedExamples;
    const expansion = expandEffectiveSpecificationExamples(application);
    application = expansion.application;
    const resolver = new AuthoringProductionResolver(application);
    const declarationSteps = new Set<SpecificationEventSyntax>();
    const exampleSpecifications = examples.flatMap(entry => {
        if (entry.kind !== 'event') {
            if ((entry.kind === 'command' || entry.kind === 'readmodel') && (entry.example.stream != null || entry.example.noStream != null))
                context.error(DiagnosticCodes.InvalidSpecificationExampleBody, "only event examples carry routes; a command's route comes from its declaration, and read models have none", (entry.example.stream ?? entry.example.noStream)!.location);
            return [];
        }
        const node: SpecificationEventSyntax = { kind: 'SpecificationEventSyntax', eventType: entry.name, values: entry.example.values, for: entry.example.for, stream: entry.example.stream, noStream: entry.example.noStream, location: entry.example.location };
        declarationSteps.add(node);
        return [{ scope: entry.scope, specification: { when: null, given: [], whenAppended: null, thenEvents: [node], whenRedelivered: null } }];
    });
    const catalog = new EventSourceCatalog(application);
    const concepts = uniqueByName(application.concepts);
    const composites = uniqueByName(application.types);
    const properties = new Map([...composites].map(([name, type]) => [name, uniqueByName(type.properties)]));
    const compatible = (value: Parameters<typeof compatibleValue>[0], type: TypeRefSyntax): boolean => compatibleValue(value, type, concepts, properties);
    const known = new Set(['String', 'Uuid', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', ...concepts.keys(), ...composites.keys()]);
    const nominallyCompatible = (source: TypeRefSyntax, target: TypeRefSyntax): boolean | null => known.has(source.name) && known.has(target.name)
        ? source.name === target.name && source.isCollection === target.isCollection && (!source.isOptional || target.isOptional) : null;
    const formatStreamId = (expression: ExpressionSyntax | null | undefined, type: TypeRefSyntax): string | null => formatSpecificationStreamId(expression, type, application);
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
            const type = commandDestinationType(command, produced, application, { resolver, slice });
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
    const validationSpecifications = [
        ...exampleSpecifications.flatMap(entry => resolver.slices.filter(owner => owner.scope.join('.') === entry.scope.join('.')).map(({ slice, scope }) => ({ slice, scope, specification: entry.specification }))),
        ...resolver.slices.flatMap(({ slice, scope }) => slice.specifications.map(specification => ({ slice, scope, specification })))
    ];
    for (const { slice, scope, specification } of validationSpecifications) {
        const commands = resolver.slices.flatMap(entry => entry.slice.commands.map(command => ({ command, scope: entry.scope })));
        const parts = specification.when?.commandType.split('.') ?? [];
        const qualifiers = parts.slice(0, -1);
        const candidates = commands.filter(entry => entry.command.name === parts.at(-1) && qualifiers.every((segment, index) => entry.scope[entry.scope.length - qualifiers.length + index] === segment));
        let command: CommandSyntax | null = qualifiers.length > 0 && candidates.length === 1 ? candidates[0].command : null;
        for (let depth = scope.length; qualifiers.length === 0 && depth >= 0 && command === null; depth--) {
            const visible = candidates.filter(entry => scope.slice(0, depth).every((segment, index) => entry.scope[index] === segment));
            if (visible.length > 0) { command = visible.length === 1 ? visible[0].command : null; break; }
        }
        const occurrences = [...specification.given.map(node => ({ node, required: true, expected: false })),
            ...(specification.whenAppended === null ? [] : [{ node: specification.whenAppended, required: true, expected: false }]),
            ...specification.thenEvents.map(node => ({ node, required: false, expected: !declarationSteps.has(node) })),
            ...(specification.whenRedelivered == null ? [] : [{ node: { ...specification.whenRedelivered, kind: 'SpecificationEventSyntax' as const }, required: false, expected: false }])];
        for (const { node, required, expected } of occurrences) {
            const step = expansion.specifications.flatMap(pair => pair.steps).find(step => step.effective === node);
            const validateRoute = step?.route?.origin !== 'example';
            const validateFor = validateRoute || node.for !== step?.example?.for;
            const contextualError = (code: string, message: string, location: typeof node.location): void => {
                context.error(code, step?.example == null ? message : `${message} Example '${step.example.name}' is used here.`, step?.example == null ? location : step.authored.location);
            };
            if (node.stream == null && node.noStream == null) continue;
            if (required && node.noStream != null)
                contextualError(DiagnosticCodes.InvalidSpecificationStream, "Expected 'stream Source.Stream', or 'no stream' on a then event.", node.noStream.location);
            let identifier: TypeRefSyntax | null = null;
            const event = eventOf(node.eventType, slice);
            const eventProducers = producers.filter(producer => producer.event === event);
            if (node.stream != null) {
                const route = node.stream;
                const resolution = catalog.resolve(route.eventSource, route.stream);
                if (resolution.kind !== EventSourceResolutionKind.Unique) {
                    const state = { ambiguous: 'Ambiguous', notFound: 'NotFound', wrongKind: 'WrongKind' }[resolution.kind];
                    if (validateRoute) context.error(DiagnosticCodes.InvalidSpecificationStreamRoute, `Stream '${route.eventSource}.${route.stream}' is ${state}; routing requires one physical source and stream.`, route.location);
                    continue;
                }
                const source = resolution.sources[0];
                const stream = resolution.streams[0];
                const validateLiteral = (mapping: PropertyMappingSyntax, target: TypeRefSyntax | null): void => {
                    const value = mapping.source;
                    const formatted = formatStreamIdLiteral(value, target, application);
                    if (formatted?.failure != null) context.error(DiagnosticCodes.InvalidSpecificationStreamRoute, streamIdFailureMessage(formatted.failure), value.location);
                    else if (value.kind !== 'LiteralExpressionSyntax' || value.value === null || target !== null && !compatible(value, target))
                        context.error(DiagnosticCodes.InvalidSpecificationStreamRoute, "A specification stream id needs a nonempty concrete scalar literal compatible with the stream's declared type.", value.location);
                };
                if (!validateRoute) { /* The authored example's route is checked once at its declaration. */ }
                else if (stream.streamIdParts.length > 0)
                    validateStreamIdParts(stream, route.streamIdParts, route.streamId !== null, route.location, DiagnosticCodes.InvalidSpecificationStreamRoute, context, validateLiteral);
                else if (route.streamIdParts.length > 0)
                    context.error(DiagnosticCodes.InvalidSpecificationStreamRoute, 'A streamId part block requires a composite stream.', route.location);
                else {
                    if ((stream.streamId === null) !== (route.streamId === null))
                        context.error(DiagnosticCodes.InvalidSpecificationStreamRoute, stream.streamId === null ? 'An unkeyed stream cannot take a streamId mapping.' : 'This keyed stream requires a streamId mapping.', route.location);
                    if (route.streamId !== null) validateLiteral(route.streamId, stream.streamId);
                }
                identifier = source.identifier;
                if (identifier === null) {
                    const types = eventProducers.map(producer => producer.type);
                    const distinct = new Set(types.filter(type => type !== null).map(type => `${type.name}:${type.isOptional}:${type.isCollection}`));
                    if (types.length === 0 || types.some(type => type === null) || distinct.size !== 1) {
                        if (validateRoute) context.error(DiagnosticCodes.InvalidSpecificationStreamEventSource, `Declare an identifier on source '${source.name}'; the event's producers do not supply one unambiguous destination type.`, route.location);
                    } else identifier = types[0];
                }
                let inheritedIdentityIsInvalid = false;
                if (step?.example?.stream != null && node.for !== null && node.for === step.example.for) {
                    const prior = catalog.resolve(step.example.stream.eventSource, step.example.stream.stream);
                    if (prior.kind === EventSourceResolutionKind.Unique) {
                        const types = eventProducers.map(producer => producer.type);
                        const distinct = new Set(types.filter(type => type !== null).map(type => `${type.name}:${type.isOptional}:${type.isCollection}`));
                        const priorIdentifier = prior.sources[0].identifier ?? (types.length > 0 && !types.some(type => type === null) && distinct.size === 1 ? types[0] : null);
                        if (priorIdentifier !== null)
                            inheritedIdentityIsInvalid = node.for.kind !== 'LiteralExpressionSyntax' || node.for.value === null || priorIdentifier.isCollection || priorIdentifier.isOptional || !compatible(node.for, priorIdentifier);
                    }
                }
                if (required && node.for === null)
                    contextualError(DiagnosticCodes.InvalidSpecificationStreamEventSource, "A routed given or when append event requires 'for <literal>'.", route.location);
                else if (validateFor && !inheritedIdentityIsInvalid && node.for !== null && (node.for.kind !== 'LiteralExpressionSyntax' || node.for.value === null || identifier !== null && (identifier.isCollection || identifier.isOptional || !compatible(node.for, identifier))))
                    contextualError(DiagnosticCodes.InvalidSpecificationStreamEventSource, "A routed event's for value must be a concrete literal compatible with the source's identifier type.", node.for.location);
            }
            if (!expected || command === null || eventProducers.length === 0 || eventProducers.some(producer => producer.command !== command)) continue;
            const commandRoute = command.stream?.propertyCandidate === null ? command.stream : null;
            let contradicts = node.noStream != null ? commandRoute != null : commandRoute == null || commandRoute.eventSource !== node.stream!.eventSource || commandRoute.stream !== node.stream!.stream;
            if (!contradicts && node.stream != null && commandRoute != null) {
                const resolution = catalog.resolve(node.stream.eventSource, node.stream.stream);
                const stream = resolution.kind === EventSourceResolutionKind.Unique ? resolution.streams[0] : null;
                if (stream !== null && stream.streamIdParts.length > 0) {
                    if (commandRoute.streamId === null && node.stream.streamId === null && commandRoute.streamIdParts.length > 0 && node.stream.streamIdParts.length > 0) {
                        for (const part of stream.streamIdParts) {
                            const actual = commandRoute.streamIdParts.filter(mapping => mapping.property === part.name);
                            const expected = node.stream.streamIdParts.filter(mapping => mapping.property === part.name);
                            if (actual.length !== 1 || expected.length !== 1) continue;
                            const actualId = formatStreamId(actual[0].source, part.type);
                            const expectedId = formatStreamId(expected[0].source, part.type);
                            if (actualId !== null && expectedId !== null && actualId !== expectedId) contradicts = true;
                        }
                    }
                } else if (stream?.streamId != null && commandRoute.streamIdParts.length === 0 && node.stream.streamIdParts.length === 0) {
                    const actualId = formatStreamId(commandRoute.streamId?.source, stream.streamId);
                    const expectedId = formatStreamId(node.stream.streamId?.source, stream.streamId);
                    if (actualId !== null && expectedId !== null && actualId !== expectedId) contradicts = true;
                }
            }
            if (node.for?.kind === 'LiteralExpressionSyntax' && eventProducers.every(producer => producer.type !== null &&
                (identifier !== null ? nominallyCompatible(producer.type, identifier) === false : !compatible(node.for!, producer.type)))) contradicts = true;
            if (contradicts) contextualError(DiagnosticCodes.SpecificationStreamContradictsCommand, 'The expected event route contradicts its only producer, the command under test.', (node.stream ?? node.noStream)!.location);
        }
    }
}
