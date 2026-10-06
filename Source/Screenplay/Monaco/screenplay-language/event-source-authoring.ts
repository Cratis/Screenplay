// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompletionEntry } from './completion-items';
import { fenceMap, indentOf, withoutComment } from './document-context';
import { AuthoredEventSource } from './AuthoredEventSource';
import { AuthoredStream } from './AuthoredStream';
import { isSourceStreamName, isSourceStreamTypeName, sourceStreamPattern } from '@cratis/screenplay-compiler';
import { DocumentSymbols, symbolsForBuffer } from './symbols';
import { responseAnalysis } from './response-analysis';
import { typeReferenceText } from './TypeReferenceSymbol';

export const eventSourceAvailability = 'Syntax-only; not admitted by any supported executable model (ESM) version yet (PLAY0268) (#302). Authored classification does not supply an identity destination. No semantic IDs or automatic identity refactors.';

const routePrefix = sourceStreamPattern('^\\s*stream\\s+(?:[A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)?\\.?)?$');
const keyPrefix = sourceStreamPattern('^\\s*streamId\\s*=\\s*([\\w.]*)$');
const identifierTypePrefix = sourceStreamPattern('^\\s*identifier\\s+[\\w.]*$');
const streamTypePrefix = sourceStreamPattern('^\\s*streamId\\s+[\\w.]*$');

export function analyzeEventSources(lines: string[], symbols?: DocumentSymbols) {
    symbols = symbolsForBuffer(lines, symbols);
    return responseAnalysis(lines, symbols?.authoringDocuments ?? symbols?.authoringSources?.filter(source => source !== lines.join('\n')) ?? [], symbols?.authoringPlacement, symbols?.authoringPath, symbols?.authoringPlacementResolved).eventSources;
}

export function eventSourceDetails(source: AuthoredEventSource, stream?: AuthoredStream): string {
    return [`**${stream ? `stream ${source.name}.${stream.name}` : `eventsource ${source.name}`}**`,
        stream?.description ?? source.description ?? '',
        source.identifier ? `Identifier type: ${typeReferenceText(source.identifier)}` : 'No identifier type declared.',
        stream ? stream.streamId ? `Stream id type: ${typeReferenceText(stream.streamId)}` : 'Unkeyed stream.' : source.streams.map(stream => `${stream.name}${stream.streamId ? ` — ${typeReferenceText(stream.streamId)}` : ' — unkeyed'}`).join('\n'),
        (stream?.id ?? (!stream ? source.id : null)) ? 'Stored-name pin (rename-only metadata; not a semantic ID).' : '', eventSourceAvailability].filter(Boolean).join('\n\n');
}

export function eventSourceReferenceAt(lines: string[], line: number, start: number, end: number, symbols?: DocumentSymbols) {
    const analysis = analyzeEventSources(lines, symbols);
    const at = (route: typeof analysis.routes[number]) => {
        const location = route.referenceLocation;
        const name = `${route.eventSource}.${route.stream}`;
        return location?.line === line + 1 && start >= location.column && end <= location.column + name.length &&
            withoutComment(lines[line] ?? '').slice(location.column - 1, location.column - 1 + name.length) === name;
    };
    const candidate = analysis.ambiguousCandidates.find(at);
    if (candidate) return { route: candidate, resolution: { state: 'ambiguous', source: undefined, stream: undefined }, target: undefined };
    const route = analysis.routes.find(at);
    if (!route) return null;
    const resolution = analysis.resolve(route.eventSource, route.stream);
    const sourcePart = end <= route.referenceLocation!.column + route.eventSource.length;
    return { route, resolution, target: resolution.state === 'unique' ? sourcePart ? resolution.source : resolution.stream : undefined };
}

// The location is a typed header start. Match its exact prefix, never search comments or another
// occurrence of the same name. Both hosts use this to navigate to the authored identifier.
export function eventSourceIdentifier(location: { line: number; column: number; path?: string }, name: string, source: string) {
    const line = withoutComment(source.split(/\r?\n/)[location.line - 1] ?? '');
    const prefix = line.slice(location.column - 1).match(/^(?:eventsource|stream)\s+/)?.[0];
    return prefix && line.slice(location.column - 1 + prefix.length, location.column - 1 + prefix.length + name.length) === name
        ? { ...location, column: location.column + prefix.length } : undefined;
}

export function eventSourceHover(lines: string[], line: number, start: number, end: number, symbols?: DocumentSymbols): string | null {
    symbols = symbolsForBuffer(lines, symbols);
    const analysis = analyzeEventSources(lines, symbols);
    const reference = eventSourceReferenceAt(lines, line, start, end, symbols);
    if (reference) return reference.resolution.source && reference.resolution.stream ? eventSourceDetails(reference.resolution.source, reference.resolution.stream)
        : `Physical stream owner: ${reference.resolution.state}; no route selected. ${'reasons' in reference.resolution ? reference.resolution.reasons.join(' ') : ''} ${eventSourceAvailability}`;
    const context = analysis.contexts.get(line);
    const declaration = context?.stream ?? context?.source;
    if (declaration) {
        const name = eventSourceIdentifier(declaration.location, declaration.name, lines.join('\n'));
        if (name?.line === line + 1 && start === name.column && end === start + declaration.name.length && context?.source) {
            const confidence = analysis.resolve(context.source.name, context.stream?.name);
            return eventSourceDetails(context.source, context.stream) + (confidence.state === 'unique' ? '' : `\n\nPhysical ownership: ${confidence.state}. ${confidence.reasons.join(' ')}`);
        }
    }
    if (context?.route?.streamId && context.command) {
        const mapping = context.route.streamId;
        if (mapping.location.line === line + 1 && start === mapping.location.column && end === start + 8)
            return `Authored stream id mapping. ${eventSourceAvailability}`;
        const expression = mapping.source as { kind?: string; path?: string; location?: { line: number; column: number } };
        if (expression.kind === 'PathExpressionSyntax' && expression.path && expression.location?.line === line + 1 &&
            start >= expression.location.column && end <= expression.location.column + expression.path.length) {
            const typed = responseAnalysis(lines, symbols?.authoringDocuments ?? symbols?.authoringSources?.filter(source => source !== lines.join('\n')) ?? [], symbols?.authoringPlacement, symbols?.authoringPath, symbols?.authoringPlacementResolved);
            const prefix = expression.path.slice(0, end - expression.location.column);
            const parts = prefix.split('.');
            let properties = context.command.properties;
            let optional = false;
            let collection = false;
            for (const [index, part] of parts.entries()) {
                const matches = properties.filter(property => property.name === part);
                if (matches.length !== 1) break;
                const property = matches[0];
                optional ||= property.type.isOptional;
                collection ||= property.type.isCollection;
                if (index === parts.length - 1)
                    return `**${part}** — ${typeReferenceText({ ...property.type, isOptional: optional, isCollection: collection })}. Command source for authored stream id. ${eventSourceAvailability}`;
                const types = typed.operations.types.filter(type => type.name === property.type.name);
                if (types.length !== 1 || collection) break;
                properties = types[0].properties.map(property => ({ ...property, isIdentifier: false }));
            }
        }
    }
    if (context?.source && /^\s*id\s+"/.test(withoutComment(lines[line] ?? '')) && start === indentOf(lines[line]) + 1 && end === start + 2)
        return `Stored-name pin: rename-only metadata, not a semantic identity or an automatic refactor. ${eventSourceAvailability}`;
    const type = context?.stream?.streamId ?? context?.source?.identifier;
    if (type && type.location?.line === line + 1 && start >= type.location.column && end <= type.location.column + type.name.length)
        return `${typeReferenceText(type)} — authored ${context?.stream ? 'stream id' : 'identifier'} type. ${eventSourceAvailability}`;
    return null;
}

export function eventSourceCompletions(lines: string[], line: number, before: string, symbols: DocumentSymbols): CompletionEntry[] | null {
    // .NET word characters are UTF-16 code units; supplementary letters are not identifiers.
    if (fenceMap(lines)[line] || withoutComment(before).length < before.length || /[\uD800-\uDFFF]/.test(before)) return null;
    const analysis = analyzeEventSources(lines, symbols);
    const context = analysis.contexts.get(line);
    const indent = before.trim() ? indentOf(lines[line]) : before.length;
    if (context?.command && indent > indentOf(lines[context.command.location.line - 1]) &&
        (!context.route || indent <= indentOf(lines[context.route.location.line - 1])) && routePrefix.test(before)) {
        // A property named stream remains legal. Only offer routes whose complete-input
        // candidates have no known value-type competitor; never silently rewrite ambiguity.
        const typed = responseAnalysis(lines, symbols.authoringDocuments ?? symbols.authoringSources?.filter(source => source !== lines.join('\n')) ?? [], symbols.authoringPlacement, symbols.authoringPath);
        const names = new Set([...typed.operations.concepts, ...typed.operations.types].map(type => type.name));
        // targets already exclude competing imports from the authoritative parsed documents.
        // Scanned symbols can retain an import removed from the current unsaved buffer.
        const qualified = before.trim().split(/\s+/)[1] ?? '';
        return analysis.targets.filter(target => isSourceStreamName(target.source.name) && isSourceStreamName(target.stream.name) && !names.has(target.name) && (!qualified.includes('.') || target.name.startsWith(qualified.slice(0, qualified.lastIndexOf('.') + 1))))
            .map(target => ({ label: target.name, insertText: qualified.includes('.') ? target.stream.name : target.name, documentation: eventSourceDetails(target.source, target.stream) }));
    }
    if (context?.route && context.command && indent > indentOf(lines[context.route.location.line - 1]) && keyPrefix.test(before)) {
        const target = analysis.resolve(context.route.eventSource, context.route.stream).stream?.streamId;
        if (!target) return [];
        const typed = responseAnalysis(lines, symbols.authoringDocuments ?? symbols.authoringSources?.filter(source => source !== lines.join('\n')) ?? [], symbols.authoringPlacement, symbols.authoringPath);
        const concepts = typed.operations.concepts.filter(concept => concept.name === target.name);
        const supported = ['String', 'Uuid'].includes(target.name) || concepts.length === 1 && ['String', 'Uuid', 'Int'].includes(concepts[0].type) && concepts[0].values.length === 0;
        if (!supported || target.isOptional || target.isCollection) return [];
        const parts = keyPrefix.exec(before)![1].split('.');
        let properties = context.command.properties;
        for (const part of parts.slice(0, -1)) {
            const matches = properties.filter(property => property.name === part);
            if (matches.length !== 1 || matches[0].type.isOptional || matches[0].type.isCollection) return [];
            const types = typed.operations.types.filter(type => type.name === matches[0].type.name);
            if (types.length !== 1) return [];
            properties = types[0].properties.map(property => ({ ...property, isIdentifier: false }));
        }
        return properties.filter(property => isSourceStreamName(property.name) && property.type.name === target.name && !property.type.isOptional && !property.type.isCollection)
            .map(property => ({ label: property.name, insertText: property.name, documentation: `${typeReferenceText(property.type)} — command source for authored stream id. ${eventSourceAvailability}` }));
    }
    if (context?.source && indent > indentOf(lines[context.source.location.line - 1])) {
        const ownsStream = context.stream && indent > indentOf(lines[context.stream.location.line - 1]);
        if ((ownsStream ? streamTypePrefix : identifierTypePrefix).test(before)) {
            const typed = responseAnalysis(lines, symbols.authoringDocuments ?? [], symbols.authoringPlacement, symbols.authoringPath);
            const streamId = /^\s*streamId\b/.test(before);
            return [...(streamId ? ['String', 'Uuid'] : ['String', 'Uuid', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime']), ...typed.operations.concepts.filter(concept => !streamId || ['String', 'Uuid', 'Int'].includes(concept.type) && concept.values.length === 0).map(concept => concept.name)]
                .filter(isSourceStreamTypeName).map(name => ({ label: name, insertText: name, documentation: eventSourceAvailability }));
        }
        if (/^\s*$/.test(before)) return ownsStream ? [
            { label: 'streamId', insertText: 'streamId ${1:Type}', documentation: eventSourceAvailability },
            { label: 'description', insertText: 'description "${1:Description}"', documentation: 'Authored description.' }
        ] : [
            { label: 'identifier', insertText: 'identifier ${1:Type}', documentation: eventSourceAvailability },
            { label: 'stream', insertText: 'stream ${1:Name}', documentation: eventSourceAvailability },
            { label: 'description', insertText: 'description "${1:Description}"', documentation: 'Authored description.' }
        ];
    }
    return null;
}
