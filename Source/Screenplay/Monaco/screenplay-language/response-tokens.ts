// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { responseAnalysis } from './response-analysis';
import { DocumentSymbols } from './symbols';
import { withoutComment } from './document-context';

export const responseTokenTypes = ['keyword', 'property', 'type'] as const;

// Semantic overlays resolve the local returns ambiguity that a line-based grammar cannot.
export function responseTokens(lines: string[], symbols?: DocumentSymbols): { line: number; column: number; length: number; type: number }[] {
    const tokens: { line: number; column: number; length: number; type: number }[] = [];
    const add = (line: number, column: number, length: number, type: number) => {
        const source = lines[line - 1];
        if (source === undefined || column < 1 || length < 1) return;
        const start = column - 1 + (source[column - 1] === '@' ? 1 : 0);
        if (start + length <= source.length) tokens.push({ line: line - 1, column: start, length, type });
    };
    const path = symbols?.authoringPath ?? 'current.play';
    const analysis = responseAnalysis(lines, symbols?.authoringDocuments ?? symbols?.authoringSources?.filter(source => source !== lines.join('\n')) ?? [], symbols?.authoringPlacement, path, symbols?.authoringPlacementResolved);
    for (const source of analysis.eventSources.declarations.filter(source => source.location.path === path)) {
        add(source.location.line, source.location.column, 11, 0);
        if (source.identifier) add(source.identifier.location.line, source.identifier.location.column, source.identifier.name.length, 2);
        for (const stream of source.streams) {
            add(stream.location.line, stream.location.column, 6, 0);
            if (stream.streamId) add(stream.streamId.location.line, stream.streamId.location.column, stream.streamId.name.length, 2);
        }
    }
    for (const route of analysis.eventSources.routes) {
        add(route.location.line, route.location.column, 6, 0);
        if (route.referenceLocation) add(route.referenceLocation.line, route.referenceLocation.column, `${route.eventSource}.${route.stream}`.length, 2);
        if (route.streamId) add(route.streamId.location.line, route.streamId.location.column, 8, 0);
    }
    for (const command of analysis.commands.values()) {
        for (const property of command.properties) {
            if (!property.isGenerated) continue;
            const source = withoutComment(lines[property.location.line - 1] ?? '');
            add(property.location.line, source.lastIndexOf('generated') + 1, 9, 0);
        }
        const response = command.response;
        if (!response) continue;
        add(response.location.line, response.location.column, 7, 0);
        if (response.kind === 'ScalarCommandResponseSyntax') {
            add(response.source.location.line, response.source.location.column, response.source.property.length, 1);
        } else for (const field of response.fields) {
            add(field.location.line, field.location.column, field.name.length, 1);
            add(field.source.location.line, field.source.location.column, field.source.property.length, 1);
            if (field.type) add(field.type.location.line, field.type.location.column, field.type.name.length, 2);
        }
    }
    for (const specification of analysis.specifications.values()) {
        for (const event of specification.thenEvents ?? []) {
            if (event.noStream) add(event.noStream.location.line, event.noStream.location.column, 9, 0);
        }
        for (const fixture of specification.when?.generatedValues ?? []) {
            add(fixture.location.line, fixture.location.column, 9, 0);
        }
        if (specification.thenReturns) add(specification.thenReturns.location.line, (lines[specification.thenReturns.location.line - 1] ?? '').indexOf('returns') + 1, 7, 0);
    }
    for (const system of analysis.operations.systems.filter(system => system.location.path === path)) {
        add(system.location.line, system.location.column, 6, 0);
    }
    for (const operation of analysis.operations.declarations.filter(operation => operation.location.path === path)) {
        const header = withoutComment(lines[operation.location.line - 1] ?? '');
        const keyword = header.indexOf('operation', operation.location.column - 1);
        if (keyword >= 0) add(operation.location.line, keyword + 1, 9, 0);
        for (const input of operation.inputs) {
            add(input.location.line, input.location.column, input.name.length, 1);
            add(input.type.location.line, input.type.location.column, input.type.name.length, 2);
        }
        if (operation.usesLocation) add(operation.usesLocation.line, (lines[operation.usesLocation.line - 1] ?? '').indexOf('uses') + 1, 4, 0);
        for (const phase of [operation.execute, operation.compensate]) {
            if (!phase) continue;
            const name = withoutComment(lines[phase.location.line - 1] ?? '').trim();
            add(phase.location.line, phase.location.column, name.length, 0);
            if (phase.implementation) add(phase.implementation.location.line, phase.implementation.location.column, 14, 0);
            for (const hint of phase.implementation?.hints ?? []) add(hint.location.line, hint.location.column, 4, 0);
        }
    }
    // Overlays never overlap or duplicate a typed token, even with recovered malformed syntax.
    const ordered = tokens.sort((left, right) => left.line - right.line || left.column - right.column);
    return ordered.filter((token, index) => index === 0 || token.line !== ordered[index - 1].line || token.column >= ordered[index - 1].column + ordered[index - 1].length);
}
