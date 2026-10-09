// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { eventBodyReservedWords } from '@cratis/screenplay-compiler';
import { fenceMap, indentOf } from './document-context';
import { eventAnalysisSource } from './event-analysis-source';
import { CommandResponseSymbol, responseAnalysis } from './response-analysis';
import { fileReferenceOn } from './file-references';
import { clauseKeywords } from './language';
import { AuthoringDocument } from './AuthoringDocument';
import { AuthoredEventSource } from './AuthoredEventSource';
import { ProductionSymbol } from './ProductionSymbol';
import { TypeReferenceSymbol, typeReferenceSymbol } from './TypeReferenceSymbol';

export interface PropertySymbol {
    name: string;
    type: string;
    typeReference?: TypeReferenceSymbol;
    // Exact spelling and UTF-16 columns in the authoring source, independent of normalization.
    sourceType?: { text: string; startColumn: number; endColumn: number };
    isIdentifier: boolean;
    isGenerated?: boolean;
    isSubject?: boolean;
    line: number;
}

export interface ConceptSymbol {
    name: string;
    primitive: string;
    attributes: string[];
    attributeReasons: Record<string, string>;
    enumValues: string[];
    line: number;
}

export interface TypeSymbol {
    name: string;
    properties: PropertySymbol[];
    line: number;
}

export interface PolicySymbol {
    name: string;
    requires: string[];
    line: number;
}

export interface EventSymbol {
    name: string;
    generation?: number;
    inline?: boolean;
    properties: PropertySymbol[];
    line: number;
}

export interface CommandSymbol extends NamedSymbol {
    properties: PropertySymbol[];
    reads?: ReadSymbol[];
    produces?: ProductionSymbol[];
    productionHeaders?: number[];
    response?: CommandResponseSymbol | null;
}

export interface ReadSymbol {
    view: string;
    alias?: string;
    by?: string;
    line: number;
}

export interface NamedSymbol {
    name: string;
    line: number;
}

export interface QuerySymbol extends NamedSymbol {
    returnType: string;
    returnTypeReference?: TypeReferenceSymbol;
    parameters: PropertySymbol[];
}

export interface ImportSymbol {
    qualifiedName: string;
    shortName: string;
    line: number;
}

export interface DocumentSymbols {
    authoringSources?: readonly string[];
    authoringPath?: string;
    authoringPlacement?: readonly string[];
    authoringPlacementResolved?: boolean;
    authoringDocuments?: readonly AuthoringDocument[];
    eventSources?: readonly AuthoredEventSource[];
    imports: ImportSymbol[];
    concepts: ConceptSymbol[];
    types: TypeSymbol[];
    policies: PolicySymbol[];
    events: EventSymbol[];
    commands: CommandSymbol[];
    queries: QuerySymbol[];
    screens: NamedSymbol[];
    triggers: NamedSymbol[];
}

const conceptPattern = /^concept\s+(\w+)\s*:\s*(\w+)((?:\s+@\w+)*)\s*$/;
const propertyPattern = /^\s*(@?[a-z_]\w*)\s+([\w.]+(?:\[\])?(?:\?|\s+optional)?)(\s+generated)?(\s+identifier)?(\s+subject)?\s*$/;
const attributeReasonPattern = /^([a-z_]\w*)\s+reason\s+"((?:[^"\\]|\\.)*)"\s*$/;
const readPattern = /^\s*reads\s+([A-Z]\w*)(?:\s+as\s+([a-z_]\w*))?(?:\s+by\s+([a-z_]\w*))?\s*$/;
const commandReserved = ['authorize', 'produces', 'reads'];
// AuthorizeParser's continuation pattern, including the compiler's Unicode word characters.
const authorizationContinuation = /^(?:(?:or|and)\s+)?[A-Za-z_(][\p{L}\p{Mn}\p{Nd}\p{Pc}\s()]*$/u;
// Use the compiler's contextual inventory, not the global completion vocabulary.
const eventReserved = eventBodyReservedWords;
const inlineEventReserved = ['tag', 'for', 'generation', 'origin', 'namespace', 'sequence', 'correlation', 'causation', 'causedBy', 'occurred'];
const queryParameterPattern =
    /^\s*(?:by|filter)\s+([a-z_]\w*)\s+([\w.]+(?:\[\])?(?:\?|\s+optional)?)(?:\s+from\s+.+)?\s*$/;

// Property-shaped clauses depend on their owner. Commands reserve only authorize,
// produces and reads; bare directives such as description and handler can be properties.
// The @ escape always denotes a property, and 'as' keeps its established property meaning.
export function propertyTypeReference(property: PropertySymbol): TypeReferenceSymbol {
    return property.typeReference ?? typeReferenceSymbol(property.type);
}

function sourceTypeAt(line: string, startColumn: number): NonNullable<PropertySymbol['sourceType']> {
    const text = line.slice(startColumn - 1).match(/^[\p{L}\p{Mn}\p{Nd}\p{Pc}.]+(?:\[\])?(?:\?|\s+optional\b)?/u)?.[0] ?? '';
    return { text, startColumn, endColumn: startColumn + text.length };
}

function propertiesIn(lines: string[], body: number[], reserved: readonly string[]): PropertySymbol[] {
    return body
        .map((index) => ({ index, match: lines[index].match(propertyPattern) }))
        .filter((entry): entry is { index: number; match: RegExpMatchArray } => entry.match !== null)
        .filter(({ match }) => match[1].startsWith('@') || match[1] === 'as' || !reserved.includes(match[1]))
        .map(({ index, match }) => ({
            name: match[1].replace(/^@/, ''),
            type: match[2],
            typeReference: typeReferenceSymbol(match[2]),
            sourceType: sourceTypeAt(lines[index], lines[index].match(/^\s*\S+\s+/)![0].length + 1),
            isIdentifier: match[4] !== undefined,
            ...(match[3] !== undefined ? { isGenerated: true } : {}),
            ...(match[5] !== undefined ? { isSubject: true } : {}),
            line: index,
        }));
}

function collectBody(lines: string[], fences: boolean[], start: number, indent: number): number[] {
    const body: number[] = [];
    for (let index = start + 1; index < lines.length; index++) {
        if (fences[index]) {
            continue;
        }
        const line = lines[index];
        if (line.trim().length === 0) continue;
        if (indentOf(line) <= indent) break;
        body.push(index);
    }
    return body;
}

// Event/production parsers accept every greater indent, not just the smallest child indent.
// Command block directives own their nested lines; properties do not. Fenced prose is never syntax.
export function directBody(lines: string[], fences: boolean[], start: number, indent: number): number[] {
    const body = collectBody(lines, fences, start, indent).filter(index => !fences[index] && !/^\s*(?:\/\/|#)/.test(lines[index]));
    if (!/^\s*command\b/.test(lines[start])) return body;
    let blockIndent: number | undefined;
    let authorization = false;
    return body.filter(index => {
        const childIndent = indentOf(lines[index]);
        const text = lines[index].trim();
        // AuthorizeParser owns only consecutive deeper lines with requirement shape.
        // Unlike opaque blocks, a nonmatching line returns control to CommandParser.
        if (blockIndent !== undefined && childIndent > blockIndent &&
            (!authorization || authorizationContinuation.test(text))) return false;
        const property = text !== 'validate csharp' && propertiesIn(lines, [index], commandReserved).length > 0;
        // Description consumes only a quoted value or a fence (already excluded above).
        const leaf = property || /^description(?:\s|$)/.test(text);
        blockIndent = leaf ? undefined : childIndent;
        authorization = /^authorize(?:\s|$)/.test(text);
        return true;
    });
}

const symbolRevisions = new Map<string, DocumentSymbols>();

export function scanDocument(lines: string[]): DocumentSymbols {
    const source = lines.join('\n');
    const cached = symbolRevisions.get(source);
    if (cached) return cached;
    const analysis = responseAnalysis(lines);
    const symbols: DocumentSymbols = {
        authoringSources: [source],
        eventSources: analysis.eventSources.declarations,
        imports: [],
        concepts: [],
        types: [],
        policies: [],
        events: [],
        commands: [],
        queries: [],
        screens: [],
        triggers: [],
    };
    // One normalization pass for every declaration, including comments on fence openers.
    lines = eventAnalysisSource(lines);
    const fences = fenceMap(lines);
    const eventLines = lines;
    const eventFences = fences;
    // Typed inline mappings share property syntax once their source expression is removed.
    // Do this once, never once per inline event.
    const eventPropertyLines = lines.map(line => line.replace(/\s*=(?!=|>).*/, ''));

    for (let index = 0; index < lines.length; index++) {
        if (fences[index]) continue;
        const line = lines[index];
        const trimmed = line.trim();
        const indent = indentOf(line);

        const importMatch = eventLines[index].trim().match(/^import\s+([\w.]+)\s*$/);
        if (importMatch && indent === 0) {
            const qualifiedName = importMatch[1];
            const shortName = qualifiedName.split('.').pop() ?? qualifiedName;
            symbols.imports.push({ qualifiedName, shortName, line: index });
            continue;
        }

        const conceptMatch = trimmed.match(conceptPattern);
        if (conceptMatch) {
            const body = collectBody(lines, fences, index, indent).map((i) => lines[i].trim());
            const attributeReasons: Record<string, string> = {};
            for (const line of body) {
                const reason = line.match(attributeReasonPattern);
                if (reason && attributeReasons[reason[1]] === undefined) {
                    attributeReasons[reason[1]] = reason[2];
                }
            }
            const enumValues =
                conceptMatch[2] === 'Enum'
                    ? body.filter((line) => !attributeReasonPattern.test(line))
                    : [];
            symbols.concepts.push({
                name: conceptMatch[1],
                primitive: conceptMatch[2],
                attributes: conceptMatch[3].trim().split(/\s+/).filter(Boolean),
                attributeReasons,
                enumValues,
                line: index,
            });
            continue;
        }

        const typeMatch = trimmed.match(/^type\s+(\w+)\s*$/);
        if (typeMatch) {
            symbols.types.push({
                name: typeMatch[1],
                properties: propertiesIn(lines, collectBody(lines, fences, index, indent), clauseKeywords),
                line: index,
            });
            continue;
        }

        const policyMatch = trimmed.match(/^policy\s+(\w+)\s*$/);
        if (policyMatch) {
            const requires = collectBody(lines, fences, index, indent)
                .filter((i) => !fences[i])
                .map((i) => lines[i].trim());
            symbols.policies.push({ name: policyMatch[1], requires, line: index });
            continue;
        }

        const eventMatch = eventLines[index].trim().match(/^(?:produces\s+)?event\s+(\w+)(?:\s+generation\s+(\d+))?\s*$/);
        if (eventMatch) {
            const inline = trimmed.startsWith('produces ');
            const body = directBody(eventLines, eventFences, index, indent);
            // Standalone events distinguish file metadata from properties by path shape.
            const propertyBody = inline ? body : body.filter(line => fileReferenceOn(eventLines[line], line) === undefined);
            symbols.events.push({
                name: eventMatch[1],
                ...(eventMatch[2] ? { generation: Number(eventMatch[2]) } : {}),
                inline,
                properties: propertiesIn(inline ? eventPropertyLines : eventLines,
                    propertyBody, inline ? inlineEventReserved : eventReserved),
                line: index,
            });
            continue;
        }

        const command = analysis.commands.get(index);
        if (command) {
            // A command property is a leaf, not an indentation owner. Share the parser-shaped
            // body with properties, productions, advice and destination hints.
            const body = directBody(lines, fences, index, indent);
            const productionHeaders = body.filter(line => /^\s*produces\b/.test(lines[line]) && !analysis.operationProductionLines?.has(line));
            symbols.commands.push({
                name: command.name,
                properties: command.properties.map(property => ({
                    name: property.name,
                    type: `${property.type.name}${property.type.isCollection ? '[]' : ''}${property.type.isOptional ? ' optional' : ''}`,
                    typeReference: { name: property.type.name, isCollection: property.type.isCollection, isOptional: property.type.isOptional },
                    sourceType: sourceTypeAt(lines[property.type.location.line - 1], property.type.location.column),
                    isIdentifier: property.isIdentifier || /\sidentifier\s*$/.test(lines[property.location.line - 1]),
                    ...(property.isGenerated ? { isGenerated: true } : {}),
                    line: property.location.line - 1,
                })),
                response: command.response,
                productionHeaders,
                produces: productionHeaders.flatMap(line => {
                    const header = eventLines[line].trim();
                    const inline = /^produces\s+event\b/.test(header);
                    const conditional = /^produces\s+when\b/.test(header);
                    const eventLine = conditional ? directBody(eventLines, eventFences, line, indentOf(eventLines[line]))[0] : line;
                    if (eventLine === undefined) return [];
                    const name = conditional ? eventLines[eventLine].trim() : header.match(inline ? /^produces\s+event\s+([A-Za-z_]\w*)(?:\s+generation\s+\d+)?\s*$/ : /^produces\s+([A-Z]\w*)\s*$/)?.[1];
                    if (name === undefined || !/^[A-Za-z_]\w*$/.test(name)) return [];
                    const children = directBody(eventLines, eventFences, eventLine, indentOf(eventLines[eventLine]));
                    const targets = children.map(index => eventLines[index].trim().match(/^for(?:\s+(.*))?$/)).filter(match => match !== null);
                    // Empty or repeated targets are uncertain, not implicit destinations.
                    const target = targets.length === 0 ? undefined : targets.length === 1 ? targets[0][1] ?? '' : '';
                    const mappings = children.flatMap(index => {
                        const match = eventLines[index].trim().match(inline ? /^(@?[a-z_]\w*)\s+[\w.]+(?:\[\])?(?:\?|\s+optional)?\s*=(?!=|>)\s*(.+)$/ : /^(@?[\w.]+)\s*=(?!=|>)\s*(.+)$/);
                        return match === null ? [] : [{ name: match[1].replace(/^@/, ''), source: match[2], line: index }];
                    });
                    return [{ name, inline, conditional, line, target, mappings }];
                }),
                reads: body.filter((line) => !fences[line])
                    .map((line) => ({ line, match: lines[line].match(readPattern) }))
                    .filter((entry): entry is { line: number; match: RegExpMatchArray } => entry.match !== null)
                    .map(({ line, match }) => ({
                        view: match[1],
                        ...(match[2] ? { alias: match[2] } : {}),
                        ...(match[3] ? { by: match[3] } : {}),
                        line,
                    })),
                line: index,
            });
            continue;
        }

        // The return type shape is the compiler's, dots and all - QueryParser's
        // ^query\s+([A-Za-z_]\w*)\s*=>\s*(observable\s+)?([\w.]+(?:\[\])?\??)$. A narrower one here
        // silently drops the query from the symbol table, and with it every check and completion that
        // reads from it.
        const queryMatch = trimmed.match(/^query\s+(\w+)\s*=>\s*(?:observable\s+)?([\w.]+(?:\[\])?(?:\?|\s+optional)?)\s*$/);
        if (queryMatch) {
            // The return type names a read model, which no construct declares — only the
            // 'by' and 'filter' parameters resolve against the document's own types.
            const parameters = collectBody(lines, fences, index, indent)
                .map((i) => ({ index: i, match: lines[i].match(queryParameterPattern) }))
                .filter(
                    (entry): entry is { index: number; match: RegExpMatchArray } =>
                        entry.match !== null,
                )
                .map(({ index: line, match }) => ({
                    name: match[1],
                    type: match[2],
                    typeReference: typeReferenceSymbol(match[2]),
                    sourceType: sourceTypeAt(lines[line], lines[line].match(/^\s*(?:by|filter)\s+\S+\s+/)![0].length + 1),
                    isIdentifier: false,
                    line,
                }));
            symbols.queries.push({
                name: queryMatch[1],
                returnType: queryMatch[2],
                returnTypeReference: typeReferenceSymbol(queryMatch[2]),
                parameters,
                line: index,
            });
            continue;
        }

        const screenMatch = trimmed.match(/^screen\s+(\w+)\s*$/);
        if (screenMatch) {
            symbols.screens.push({ name: screenMatch[1], line: index });
            continue;
        }

        const triggerMatch = trimmed.match(/^trigger\s+(\w+)\s*$/);
        if (triggerMatch && indent === 0) {
            symbols.triggers.push({ name: triggerMatch[1], line: index });
        }
    }

    if (symbolRevisions.size >= 16) symbolRevisions.delete(symbolRevisions.keys().next().value!);
    symbolRevisions.set(source, symbols);
    return symbols;
}

// The symbols of several documents as one - what an application spread over files declares. Lines refer to
// the document each symbol came from, so the result names things; it does not locate them.
export function mergeSymbols(...documents: DocumentSymbols[]): DocumentSymbols {
    return {
        authoringSources: documents.flatMap(document => document.authoringSources ?? []),
        ...(documents.some(document => document.authoringDocuments) ? { authoringDocuments: documents.flatMap(document => document.authoringDocuments ?? []) } : {}),
        eventSources: documents.flatMap(document => document.eventSources ?? []),
        imports: documents.flatMap((document) => document.imports),
        concepts: documents.flatMap((document) => document.concepts),
        types: documents.flatMap((document) => document.types),
        policies: documents.flatMap((document) => document.policies),
        events: documents.flatMap((document) => document.events),
        commands: documents.flatMap((document) => document.commands),
        queries: documents.flatMap((document) => document.queries),
        screens: documents.flatMap((document) => document.screens),
        triggers: documents.flatMap((document) => document.triggers),
    };
}

// Name scans are not physical input documents. Keep the host's authoritative paths separate,
// and let responseAnalysis replace the current document with this buffer exactly once.
export function symbolsForBuffer(lines: string[], application?: DocumentSymbols): DocumentSymbols {
    const path = application?.authoringPath ?? 'current.play';
    const current = application?.authoringDocuments?.find(document => document.path === path);
    return { ...mergeSymbols(scanDocument(lines), application ?? mergeSymbols()),
        authoringDocuments: application?.authoringDocuments,
        authoringPath: path,
        authoringPlacement: application?.authoringPlacement ?? current?.placement,
        authoringPlacementResolved: application?.authoringPlacementResolved ?? current?.isPlacementResolved,
    };
}

export function knownEventNames(symbols: DocumentSymbols): string[] {
    return [
        ...symbols.events.map((event) => event.name),
        ...symbols.imports.map((imported) => imported.shortName),
    ];
}

// The built-in host signals, which have no declaration to scan for because every application has them.
export const builtInTriggerNames = ['Startup', 'Shutdown'];

// What a reaction's `when` may name: an event, a declared trigger, or a host signal. The compiler resolves
// the three in that order, and the editor offers them the same way rather than guessing which was meant.
export function knownTriggerNames(symbols: DocumentSymbols): string[] {
    return [
        ...knownEventNames(symbols),
        ...symbols.triggers.map((trigger) => trigger.name),
        ...builtInTriggerNames,
    ];
}

// Everything a property type reference can resolve to — the primitives, the declared
// concepts and types, and the short names of the imports.
export function knownTypeNames(symbols: DocumentSymbols, primitives: readonly string[]): string[] {
    return [
        ...primitives,
        ...symbols.concepts.map((concept) => concept.name),
        ...symbols.types.map((type) => type.name),
        ...symbols.imports.map((imported) => imported.shortName),
    ];
}
