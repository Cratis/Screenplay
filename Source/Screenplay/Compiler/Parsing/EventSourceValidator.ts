// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { EventSourceCatalog } from '../Syntax/EventSourceCatalog';
import { CommandStreamSyntax, EventSourceResolutionKind } from '../Syntax/EventSources';
import { validateObserverFilters } from './ObserverFilterValidator';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { StreamIdFormatResult, streamIdFailureMessage, tryFormatInteger, tryFormatText, tryFormatUuidText } from '../Syntax/StreamIdFormatter';
import { commandDestinationType } from './CommandDestinationTypes';
import { validateStreamIdParts } from './CompositeStreamIdValidator';
import { ParserContext } from './ParserContext';
import { compatibleValue, uniqueByName } from './ResponseValidator';
import { validateSpecificationStreams } from './SpecificationStreamValidator';

const sameExpression = (left: ExpressionSyntax | null | undefined, right: ExpressionSyntax | null | undefined): boolean =>
    left == null && right == null || left?.kind === 'PathExpressionSyntax' && right?.kind === 'PathExpressionSyntax' && left.path === right.path ||
    left?.kind === 'LiteralExpressionSyntax' && right?.kind === 'LiteralExpressionSyntax' && JSON.stringify(left.value) === JSON.stringify(right.value);
const sameRoute = (left: CommandStreamSyntax, right: CommandStreamSyntax): boolean => left.eventSource === right.eventSource && left.stream === right.stream &&
    sameExpression(left.streamId?.source, right.streamId?.source) && left.streamIdParts.length === right.streamIdParts.length &&
    left.streamIdParts.every(part => right.streamIdParts.some(other => part.property === other.property && sameExpression(part.source, other.source)));

const primitives = new Set(['String', 'Uuid', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime']);

export function supportsStreamIdentity(type: TypeRefSyntax, application: ApplicationSyntax): boolean {
    const concepts = application.concepts.filter(concept => concept.name === type.name);
    const primitive = concepts.length === 1 ? concepts[0].type : concepts.length === 0 && primitives.has(type.name) ? type.name : null;
    return primitive === 'String' || primitive === 'Uuid' || primitive === 'Int' && concepts.length === 1 && concepts[0].values.length === 0;
}

export function formatStreamIdLiteral(expression: ExpressionSyntax | null | undefined, type: TypeRefSyntax | null, application: ApplicationSyntax): StreamIdFormatResult | null {
    if (expression?.kind !== 'LiteralExpressionSyntax') return null;
    const value = expression.value;
    const primitive = type === null ? null : uniqueByName(application.concepts).get(type.name)?.type ?? type.name;
    if (typeof value === 'string') return primitive === 'Uuid' ? tryFormatUuidText(value) : tryFormatText(value);
    if (typeof value === 'number') return tryFormatInteger(value);
    if (typeof value === 'object' && value !== null && value.literalType === 'ExactNumber') {
        if (value.value.includes('.')) return tryFormatInteger(NaN);
        return tryFormatInteger(BigInt(value.value), false);
    }
    return null;
}

export function validateEventSources(application: ApplicationSyntax, context: ParserContext): void {
    const concepts = uniqueByName(application.concepts);
    const composites = uniqueByName(application.types);
    const known = new Set([...primitives, ...concepts.keys(), ...composites.keys()]);
    const compositeProperties = new Map([...composites].map(([name, type]) => [name, uniqueByName(type.properties)]));
    const compatible = (source: TypeRefSyntax, target: TypeRefSyntax): boolean | null => known.has(source.name) && known.has(target.name)
        ? source.name === target.name && source.isCollection === target.isCollection && (!source.isOptional || target.isOptional) : null;
    const validateType = (type: TypeRefSyntax | null, streamId: boolean): void => {
        if (type === null) return;
        if (type.isOptional || type.isCollection || application.types.some(composite => composite.name === type.name)) {
            context.error(DiagnosticCodes.InvalidEventSourceDeclaration, 'Identifier and streamId declarations require a nonoptional scalar value type.', type.location);
            return;
        }
        const named = application.concepts.filter(concept => concept.name === type.name);
        const primitive = named.length === 1 ? named[0].type : named.length === 0 && primitives.has(type.name) ? type.name : null;
        if (streamId && primitive !== null && !supportsStreamIdentity(type, application))
            context.error(DiagnosticCodes.UnsupportedStreamIdType, 'Stream ids support text and UUID values and their nominal concepts, plus integer-backed concepts; other types need a future portable formatter.', type.location);
        if (primitive === null && !application.imports.some(imported => imported.qualifiedName === type.name || imported.qualifiedName.split('.').at(-1) === type.name))
            context.warning(DiagnosticCodes.UnknownType, `Unknown type '${type.name}' in event source declaration.`, type.location);
    };
    const names = new Set<string>();
    for (const source of application.eventSources ?? []) {
        if (names.has(source.name)) context.error(DiagnosticCodes.InvalidEventSourceDeclaration, `Event source '${source.name}' has multiple physical declarations.`, source.location);
        names.add(source.name);
        validateType(source.identifier, false);
        const streams = new Set<string>();
        for (const stream of source.streams) {
            if (streams.has(stream.name)) context.error(DiagnosticCodes.InvalidEventSourceDeclaration, `Stream '${source.name}.${stream.name}' has multiple declarations under this source.`, stream.location);
            streams.add(stream.name);
            validateType(stream.streamId, true);
            const parts = stream.streamIdParts;
            if (parts.length > 0) {
                if (parts.length < 2) context.error(DiagnosticCodes.InvalidEventSourceDeclaration, 'A composite stream id requires at least two named parts.', stream.directiveLocations?.streamId ?? stream.location);
                for (const name of new Set(parts.map(part => part.name))) {
                    for (const duplicate of parts.filter(part => part.name === name).slice(1))
                        context.error(DiagnosticCodes.InvalidEventSourceDeclaration, `Stream id part '${duplicate.name}' is declared more than once.`, duplicate.location);
                }
                for (const part of parts) validateType(part.type, true);
            }
        }
    }
    const pathProperty = (properties: readonly PropertySyntax[] | null, path: string): { property: PropertySyntax | null; missing: boolean } => {
        const [name, ...remaining] = path.split('.');
        if (properties === null) return { property: null, missing: false };
        const matches = properties.filter(property => property.name === name);
        if (matches.length !== 1) return { property: null, missing: matches.length === 0 };
        const property = matches[0];
        if (remaining.length === 0) return { property, missing: false };
        if (primitives.has(property.type.name) || concepts.has(property.type.name) && !composites.has(property.type.name)) return { property: null, missing: true };
        const resolved = pathProperty(composites.get(property.type.name)?.properties ?? null, remaining.join('.'));
        return resolved.property === null ? resolved : { property: { ...resolved.property, type: { ...resolved.property.type, isCollection: property.type.isCollection || resolved.property.type.isCollection, isOptional: property.type.isOptional || resolved.property.type.isOptional } }, missing: false };
    };
    const catalog = new EventSourceCatalog(application);
    const resolver = new AuthoringProductionResolver(application);
    validateObserverFilters(application, context, resolver);
    for (const { slice } of resolver.slices) for (const command of slice.commands) for (const { route, production } of [
        ...(command.stream == null ? [] : [{ route: command.stream, production: null }]),
        ...command.produces.filter(produced => produced.stream != null).map(produced => ({ route: produced.stream!, production: produced }))
    ]) {
        if (route.propertyCandidate !== null) continue;
        if (production !== null && command.stream != null && sameRoute(route, command.stream))
            context.warning(DiagnosticCodes.RedundantProductionRoute, 'The production route repeats its command route; omit the override.', route.location);
        const resolution = catalog.resolve(route.eventSource, route.stream);
        if (resolution.kind !== EventSourceResolutionKind.Unique) {
            const state = { ambiguous: 'Ambiguous', notFound: 'NotFound', wrongKind: 'WrongKind' }[resolution.kind];
            context.error(DiagnosticCodes.InvalidCommandStream, `Stream '${route.eventSource}.${route.stream}' is ${state}; routing requires one physical source and stream.`, route.location);
            continue;
        }
        const source = resolution.sources[0];
        const stream = resolution.streams[0];
        const identifiers = command.properties.filter(property => property.isIdentifier);
        if (production === null && source.identifier !== null && identifiers.length === 1 && compatible(identifiers[0].type, source.identifier) === false)
            context.error(DiagnosticCodes.InvalidCommandStream, `Command identifier '${identifiers[0].name}' does not have the source's nominal identifier type '${source.identifier.name}'. The stream does not supply a destination.`, identifiers[0].location);
        for (const produced of command.produces.filter(produced => resolver.isEventProduction(produced, slice) && (production === null ? produced.stream == null : produced === production))) {
            if (source.identifier === null) continue;
            const destination = commandDestinationType(command, produced, application, { resolver, slice });
            if (destination === null) continue;
            if (production === null && produced.for === null && produced.inlineEvent !== null && identifiers.length === 1 && compatible(identifiers[0].type, source.identifier) === false) continue;
            const allocated = produced.for === null && produced.inlineEvent === null && !command.properties.some(property => property.isGenerated && property.isIdentifier);
            const primitive = concepts.get(source.identifier.name)?.type ?? (primitives.has(source.identifier.name) ? source.identifier.name : null);
            const matches = allocated ? primitive === null ? null : primitive === 'Uuid' : compatible(destination, source.identifier);
            if (matches === false) context.error(DiagnosticCodes.InvalidCommandStream, `Command production destination does not have the source's nominal identifier type '${source.identifier.name}'. The stream does not supply a destination.`, production?.stream?.location ?? produced.for?.location ?? produced.location);
        }
        const validateMapping = (mapping: PropertyMappingSyntax, target: TypeRefSyntax): void => {
            const expression = mapping.source;
            if (expression.kind === 'PathExpressionSyntax') {
                const resolved = pathProperty(command.properties, expression.path);
                if (resolved.missing || resolved.property !== null && compatible(resolved.property.type, target) === false)
                    context.error(DiagnosticCodes.InvalidCommandStream, `Stream id source '${expression.path}' is absent or incompatible with nominal type '${target.name}'.`, expression.location);
            } else if (expression.kind === 'LiteralExpressionSyntax' && expression.value !== null) {
                const formatted = formatStreamIdLiteral(expression, target, application);
                if (formatted?.failure != null) context.error(DiagnosticCodes.InvalidCommandStream, streamIdFailureMessage(formatted.failure), expression.location);
                else if (!compatibleValue(expression, target, concepts, compositeProperties)) context.error(DiagnosticCodes.InvalidCommandStream, `Stream id value is incompatible with nominal type '${target.name}'.`, expression.location);
            } else if (['RawExpressionSyntax', 'ObjectExpressionSyntax', 'ListExpressionSyntax'].includes(expression.kind) || expression.kind === 'LiteralExpressionSyntax' && expression.value === null)
                context.error(DiagnosticCodes.InvalidCommandStream, 'A stream id needs a scalar value source, not a raw expression, collection or absence.', expression.location);
        };
        if (stream.streamIdParts.length > 0) {
            validateStreamIdParts(stream, route.streamIdParts, route.streamId !== null, route.location, DiagnosticCodes.InvalidCommandStream, context, validateMapping);
            continue;
        }
        if (route.streamIdParts.length > 0) {
            context.error(DiagnosticCodes.InvalidCommandStream, 'A streamId part block requires a composite stream.', route.location);
            continue;
        }
        if (stream.streamId === null && route.streamId !== null || stream.streamId !== null && route.streamId === null)
            context.error(DiagnosticCodes.InvalidCommandStream, stream.streamId === null ? 'An unkeyed stream cannot take a streamId mapping.' : 'This keyed stream requires a streamId mapping.', route.location);
        if (stream.streamId === null || route.streamId === null) continue;
        validateMapping(route.streamId, stream.streamId);
    }
    validateSpecificationStreams(application, context);
}
