// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCode, diagnosticCodes } from './diagnostic-codes';
import { enclosingChain, fenceMap, indentOf } from './document-context';
import { productionDestinations } from './production-destinations';
import { eventAnalysisSource } from './event-analysis-source';
import { directBody, DocumentSymbols } from './symbols';
import type { ValidationIssue, ValidationSeverity } from './validation';

const reserved = new Set(['namespace', 'sequence', 'correlation', 'causation', 'causedBy', 'occurred']);
const quotedId = /^id\s+"((?:[^"\\]|\\.)*)"$/;

export function validateInlineEvents(lines: string[], symbols: DocumentSymbols, application: DocumentSymbols): ValidationIssue[] {
    lines = eventAnalysisSource(lines);
    const issues: ValidationIssue[] = [];
    const fences = fenceMap(lines);
    const report = (line: number, code: DiagnosticCode, message: string, severity: ValidationSeverity = 'error') => {
        issues.push({ line, startColumn: indentOf(lines[line]) + 1, endColumn: lines[line].length + 1, code, message, severity });
    };
    for (const event of symbols.events) {
        const body = directBody(lines, fences, event.line, indentOf(lines[event.line]));
        if (event.inline) {
            const properties = new Set<string>();
            for (const property of event.properties) {
                if (properties.has(property.name)) report(property.line, diagnosticCodes.duplicateDeclaration, `Event '${event.name}' already declares property '${property.name}'.`);
                properties.add(property.name);
            }
            const chain = enclosingChain(lines, fences, event.line, indentOf(lines[event.line]));
            if (chain[0] !== 'command') report(event.line, diagnosticCodes.inlineEventOutsideCommand, 'Inline events can only be declared inside commands.');
            if (event.generation !== undefined) report(event.line, diagnosticCodes.inlineEventGeneration, 'Inline events are generation 1; extract the event before declaring generations.');
            if ([...symbols.events, ...application.events].filter(other => other.name === event.name).length > 1 ||
                [...symbols.imports, ...application.imports].some(other => other.shortName === event.name)) {
                report(event.line, diagnosticCodes.inlineEventCollision, `Inline event '${event.name}' collides with another declaration or import.`);
            }
        }
        let hasId = false;
        let hasDocumentation = false;
        for (const index of body) {
            const text = lines[index].trim();
            const keyword = text.split(/\s+/)[0];
            if (event.inline && keyword === 'generation') report(index, diagnosticCodes.inlineEventGeneration, 'Inline events are generation 1; extract the event before declaring generations.');
            if (event.inline && keyword === 'origin') report(index, diagnosticCodes.reservedProductionMetadata, 'An inline event is local to its command and cannot declare origin.');
            // Property-shaped metadata names stay properties, whether standalone or typed mappings.
            if (/^(?:id|description|documentation)\s+[\w.]+(?:\[\])?\??(?:\s*=.*)?$/.test(text)) continue;
            if (keyword === 'id') {
                const match = text.match(quotedId);
                if (hasId || match === null || match[1].trim().length === 0) report(index, diagnosticCodes.invalidEventId, 'Expected one nonempty id "<old-name>".');
                else if (match[1] === event.name) report(index, diagnosticCodes.redundantEventId, 'This id repeats the event name; remove it.', 'information');
                hasId = true;
            }
            if (keyword === 'documentation') {
                let next = index + 1;
                while (next < lines.length && lines[next].trim().length === 0) next++;
                let closing = next + 1;
                while (closing < lines.length && lines[closing].trim() !== '```') closing++;
                if (hasDocumentation || text !== 'documentation' || lines[next]?.trim() !== '```markdown' || indentOf(lines[next] ?? '') <= indentOf(lines[index]) ||
                    closing === lines.length || lines.slice(next + 1, closing).join('\n').trim().length === 0) {
                    report(index, diagnosticCodes.invalidEventDocumentation, 'Expected one nonempty fenced markdown documentation block.');
                }
                hasDocumentation = true;
            }
        }
    }
    for (let index = 0; index < lines.length; index++) {
        if (fences[index]) continue;
        const keyword = lines[index].trim().split(/\s+/)[0];
        if (reserved.has(keyword) && enclosingChain(lines, fences, index, indentOf(lines[index])).includes('produces')) {
            report(index, diagnosticCodes.reservedProductionMetadata, `'${keyword}' is system-assigned production metadata.`);
        }
    }
    for (const command of symbols.commands) {
        const { identifier, mixed } = productionDestinations(command);
        for (const production of command.produces ?? []) {
            if (mixed && production.target === undefined) report(production.line, diagnosticCodes.explicitProducesTargetsRequired, `Production '${production.name}' must state for explicitly when destinations differ.`);
            const destination = production.target ?? (production.inline ? identifier : undefined);
            if (identifier === undefined || destination !== identifier) continue;
            for (const mapping of production.mappings.filter(mapping => mapping.source === identifier)) {
                report(mapping.line, diagnosticCodes.eventSourceIdInPayload, `The payload copies '${identifier}', which already identifies the event source.`, production.inline ? 'warning' : 'information');
            }
        }
    }
    return issues;
}
