// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { enclosingChain, fenceMap, indentOf, nearestEnclosingLine, withoutComment } from './document-context';
import { CompletionEntry, optionalTypeItems } from './completion-items';
import { DocumentSymbols, PropertySymbol, propertyTypeReference, scanDocument } from './symbols';
import { typeReferenceText } from './TypeReferenceSymbol';
import { responseAvailability, responseAnalysis } from './response-analysis';
import { primitiveTypes } from './language';

export function responseCompletions(lines: string[], line: number, before: string, symbols: DocumentSymbols): CompletionEntry[] | null {
    const fences = fenceMap(lines);
    if (fences[line] || withoutComment(before).length < before.length) return null;
    const indent = before.trim() === '' ? before.length : indentOf(lines[line]);
    const chain = enclosingChain(lines, fences, line, indent);
    const enclosing = nearestEnclosingLine(lines, fences, line, indent);
    const namedCommand = (name: string | undefined) => {
        const matches = symbols.commands.filter(command => command.name === name);
        return matches.length === 1 ? matches[0] : undefined;
    };
    const entry = (property: PropertySymbol, insertText = property.name): CompletionEntry => ({
        label: property.name, insertText,
        documentation: `${typeReferenceText(propertyTypeReference(property))}${property.isGenerated ? ' — generated, not request/form input' : ''}. ${responseAvailability}`,
    });
    if (chain.includes('command')) {
        // Merged symbol line offsets belong to different documents. Only the current (possibly
        // unsaved) source can own the cursor; application symbols are for external lookup.
        const command = scanDocument(lines).commands.filter(command => command.line < line).at(-1);
        if (!command) return null;
        if (chain[0] === 'command') {
            if (/^\s*returns\s+@?\w*$/.test(before)) {
                if (!/returns\s+@/.test(before) && command.properties.some(property => property.name === 'returns' && property.line === line)) return null;
                return command.properties.map(property => entry(property, `${/returns\s+@/.test(before) ? '' : '@'}${property.name}`));
            }
            const modifier = before.match(/^\s*@?[a-z_]\w*\s+([\w.]+)\s+(\w*)$/);
            if (modifier && ['generated', 'identifier'].some(word => word.startsWith(modifier[2]))) {
                const concept = symbols.concepts.find(concept => concept.name === modifier[1]);
                if (concept?.primitive === 'Uuid') return [
                    ...('optional'.startsWith(modifier[2]) ? optionalTypeItems : []),
                    { label: 'generated', insertText: 'generated', documentation: `Required scalar Uuid concept with no validation rules; not a request/form input. ${responseAvailability}` },
                    { label: 'generated identifier', insertText: 'generated identifier', documentation: `Specification fixture uses an indented for value. ${responseAvailability}` },
                    { label: 'identifier', insertText: 'identifier', documentation: 'Names the event source identifier.' },
                ];
            }
            if (/^\s*@?[a-z_]\w*\s+[\w.]+\s+generated\s+\w*$/.test(before)) return [{ label: 'identifier', insertText: 'identifier', documentation: responseAvailability }];
        }
        if (chain[0] === 'returns') {
            if (/=\s*@?\w*$/.test(before)) return command.properties.map(property => entry(property));
            if (/^\s*@?[a-z_]\w*\s+[\w.]*$/.test(before)) return [...symbols.concepts.map(concept => concept.name), ...symbols.types.map(type => type.name), ...primitiveTypes].map(name => ({ label: name, insertText: name, documentation: `Explicit response type must exactly match the source. ${responseAvailability}` }));
            return command.properties.map(property => entry(property, `${property.name} = ${property.name}`));
        }
    }
    if (chain[0] === 'form' && enclosing && (/^\s*field\s+\w*$/.test(before) || before.trim() === '')) {
        const command = namedCommand(enclosing.match(/\bfor\s+([\w.]+)$/)?.[1]);
        if (command) return command.properties.filter(property => !property.isGenerated).map(property => entry(property, before.trim() === '' ? `field ${property.name}` : property.name));
    }
    if (chain.includes('specification')) {
        if (chain[0] === 'when' && enclosing) {
            const name = enclosing.match(/^when\s+([\w.]+)$/)?.[1];
            const command = namedCommand(name);
            if (!command) return null;
            if (/^\s*generated\s+\w*$/.test(before)) return command.properties.filter(property => property.isGenerated && !property.isIdentifier).map(property => entry(property, `${property.name} = "\${1:uuid}"`));
            if (before.trim() === '') return [
                ...command.properties.filter(property => !property.isGenerated).map(property => entry(property, `${property.name} = \${1:value}`)),
                ...command.properties.filter(property => property.isGenerated && !property.isIdentifier).map(property => ({ ...entry(property, `generated ${property.name} = "\${1:uuid}"`), label: `generated ${property.name}` })),
                ...(command.properties.some(property => property.isGenerated && property.isIdentifier) ? [{ label: 'for', insertText: 'for "${1:uuid}"', documentation: `Generated identifier fixture, not request input. ${responseAvailability}` }] : []),
            ];
        }
        if (chain[0] === 'then' && /^then\s+returns$/.test(enclosing ?? '')) {
            const specification = [...responseAnalysis(lines).specifications.values()].filter(specification => specification.location.line - 1 < line).at(-1);
            const command = namedCommand(specification?.when?.commandType);
            if (command?.response?.kind === 'RecordCommandResponseSyntax') {
                const properties = new Map(command.properties.map(property => [property.name, property]));
                return command.response.fields.map(field => {
                    const source = properties.get(field.source.property);
                    const type = field.type ?? (source ? propertyTypeReference(source) : undefined);
                    return { label: field.name, insertText: `${field.name} = \${1:value}`, documentation: `${type ? typeReferenceText(type) : 'Unresolved type'}. ${responseAvailability}` };
                });
            }
        }
    }
    return null;
}
