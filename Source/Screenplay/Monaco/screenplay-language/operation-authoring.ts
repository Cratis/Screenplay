// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompletionEntry, operationItems, operationPhaseItems, operationImplementationItems } from './completion-items';
import { enclosingChain, fenceMap, indentOf, nearestEnclosingLine, withoutComment } from './document-context';
import { DocumentSymbols } from './symbols';
import { responseAnalysis } from './response-analysis';
import { OperationAnalysis, OperationDeclaration, OperationPhase } from './OperationAnalysis';
import { TypeReferenceSymbol, typeReferenceText } from './TypeReferenceSymbol';

export const operationAvailability = 'Syntax-only; execution unavailable until ESM v9 (PLAY0268). No semantic or requirement identity is assigned.';

export function analyzeOperations(lines: string[], symbols?: DocumentSymbols) {
    return responseAnalysis(lines, symbols?.authoringDocuments ?? symbols?.authoringSources?.filter(source => source !== lines.join('\n')) ?? [],
        symbols?.authoringPlacement, symbols?.authoringPath).operations;
}

export function phaseState(phase: OperationPhase | null): string {
    return !phase ? 'not declared' : phase.file ? `file ${phase.file.path}` : phase.code ? `inline ${phase.code.language}` : 'pending';
}

export function operationDetails(operation: OperationDeclaration): string {
    const inputs = operation.inputs.map(input => `${input.name} ${typeReferenceText(input.type)}`).join('\n');
    const phase = (name: string, value: OperationPhase | null) => `${name}: ${name === 'compensate' && !value ? 'not declared' : phaseState(value)}${value?.description ? ` — ${value.description}` : ''}${value?.implementation?.hints.map(hint => `\n  hint: ${hint.text}`).join('') ?? ''}`;
    return `**operation ${[...operation.scope, operation.name].join('.')}**\n\n${operation.description ?? ''}\n\nUses ${operation.uses}\n\n\`\`\`screenplay\n${inputs}\n\`\`\`\n\n${phase('execute', operation.execute)}\n\n${phase('compensate', operation.compensate)}\n\n${operationAvailability}`;
}

export function operationCompletions(lines: string[], line: number, before: string, symbols: DocumentSymbols): CompletionEntry[] | null {
    const fences = fenceMap(lines);
    if (fences[line] || withoutComment(before).length < before.length) return null;
    const indent = before.trim() === '' ? before.length : indentOf(lines[line]);
    const chain = enclosingChain(lines, fences, line, indent);
    const enclosing = nearestEnclosingLine(lines, fences, line, indent) ?? '';
    const analysis = responseAnalysis(lines, symbols.authoringDocuments ?? symbols.authoringSources?.filter(source => source !== lines.join('\n')) ?? [], symbols.authoringPlacement, symbols.authoringPath);
    const operations = analysis.operations;
    const context = operations.contexts.get(line);
    const targets = () => {
        if (!context) return [];
        const reference = before.match(/(?:produces|given\s+operation|then\s+(?:operation|compensated))\s+([\w.]*)$/)?.[1] ?? before.trim();
        const qualifiers = reference.split('.').slice(0, -1);
        return operations.targets(context.scope).filter(target => qualifiers.length === 0 ||
            qualifiers.length <= target.declaration.scope.length && qualifiers.every((part, index) => target.declaration.scope[target.declaration.scope.length - qualifiers.length + index] === part))
            .map(target => ({ label: target.name, insertText: qualifiers.length > 0 ? target.declaration.name : target.name, documentation: operationDetails(target.declaration) }));
    };
    if (chain.includes('specification') && /^\s*(?:given\s+operation|then\s+(?:operation|compensated))\s+[\w.]*$/.test(before)) return targets();
    if (chain[0] === 'command' && /^\s*produces\s+[\w.]*$/.test(before)) return targets().length ? [
        { label: 'event', insertText: 'event ${1:Name}', documentation: 'Declares an inline event.' },
        { label: 'operation', insertText: 'operation ${1:Name}\n    uses ${2:System}\n    ${3:input} ${4:Type} = ${5:source}', documentation: operationAvailability },
        ...symbols.events.map(event => ({ label: event.name, insertText: event.name, documentation: 'Declared event.' })), ...targets()
    ] : null;
    if (context?.commandLine !== undefined && chain[0] === 'produces' && /^produces\s+when\b/.test(enclosing) && /^\s*[\w.]*$/.test(before)) return targets();
    const ownedOperation = context?.operation;
    const operation = ownedOperation && (line === ownedOperation.location.line - 1 || indent > indentOf(lines[ownedOperation.location.line - 1])) ? ownedOperation : undefined;
    if (operation && /^\s*uses\s+\w*$/.test(before) && !operation.inputs.some(input => input.location.line === line + 1)) return operations.systems.map(system => ({ label: system.name, insertText: system.name, documentation: `${system.description ?? 'External system'}. ${operationAvailability}` }));
    if (chain[0] === 'implementation' && ['execute', 'compensate'].includes(chain[1])) return operationImplementationItems;
    if (['execute', 'compensate'].includes(chain[0]) && operation) return operationPhaseItems;
    const production = context?.production;
    const targetLine = (production?.targetLocation ?? production?.location)?.line;
    const inProductionBody = targetLine !== undefined && line > targetLine - 1 && indent > indentOf(lines[targetLine - 1]);
    const mappedOperation = operation ?? (inProductionBody ? production?.declaration : undefined);
    // Typed ranges include blank lines and comments. The caret must still be inside
    // the target's body, not dedented to a command member or conditional target line.
    // Ancestor keywords cannot own mappings, including deeper composite paths.
    if (mappedOperation) {
        const rhs = before.match(/=\s*([\w.]*)$/);
        if (rhs && context?.commandLine !== undefined) {
            const command = analysis.commands.get(context.commandLine);
            let properties: readonly { name: string; type: TypeReferenceSymbol; isGenerated?: boolean }[] = command?.properties ?? [];
            const parts = rhs[1].split('.');
            for (const part of parts.slice(0, -1)) {
                const property = properties.find(property => property.name === part);
                if (!property || property.type.isCollection) return [];
                const types = operations.types.filter(type => type.name === property.type.name);
                if (types.length !== 1) return [];
                properties = types[0].properties;
            }
            return properties.map(property => ({ label: property.name, insertText: property.name, documentation: `${typeReferenceText(property.type)}${property.isGenerated ? ' — generated source, not request input' : ''}. ${operationAvailability}` }));
        }
        if (operation && /^\s*@?[a-z_]\w*\s+[\w.]+(?:\[\])?\s+\w*$/.test(before)) return [{ label: 'optional', insertText: 'optional', documentation: 'Allows absence of the complete operation input value.' }];
        if (/^\s*@?[a-z_]\w*\s+[\w.[\]?]*$/.test(before) && operation) return [...operations.concepts.map(concept => concept.name), ...operations.types.map(type => type.name), 'String', 'Uuid', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'].map(name => ({ label: name, insertText: name, documentation: 'Operation input type.' }));
        if (operation) return operationItems;
        return mappedOperation.inputs.map(input => ({ label: input.name, insertText: `${input.name} = \${1:source}`, documentation: `${typeReferenceText(input.type)}. ${operationAvailability}` }));
    }
    if (chain[0] === 'then' && /^then\s+operation\s+/.test(enclosing)) {
        const name = enclosing.match(/^then\s+operation\s+([\w.]+)/)?.[1];
        const declaration = operations.references.filter(reference => reference.name === name && reference.location.line - 1 < line && reference.kind === 'operation').at(-1)?.declaration;
        if (declaration) {
            const value = before.match(/^\s*(\w+)\s*=\s*[^=]*$/);
            if (value) {
                const type = declaration.inputs.find(input => input.name === value[1])?.type;
                const concept = operations.concepts.filter(concept => concept.name === type?.name);
                if (concept.length === 1 && concept[0].type === 'Enum') return concept[0].values.map(value => ({ label: value, insertText: JSON.stringify(value), documentation: `${type?.name} value.` }));
            }
            return declaration.inputs.map(input => ({ label: input.name, insertText: `${input.name} = \${1:value}`, documentation: `${typeReferenceText(input.type)}. Partial assertion; ${operationAvailability}` }));
        }
    }
    return null;
}

// Shared by both editor hosts. Only the parser-owned target span can bind a reference;
// a repeated identifier in a trailing comment or a condition is not that reference.
export function operationReferenceAt(analysis: OperationAnalysis, lines: string[], line: number, start: number, end: number) {
    const source = withoutComment(lines[line] ?? '');
    return analysis.references.find(reference => {
        const target = reference.targetLocation ?? reference.location;
        return target.line === line + 1 && start >= target.column && end <= target.column + reference.name.length &&
            source.slice(target.column - 1, target.column - 1 + reference.name.length) === reference.name;
    });
}

export function operationHover(lines: string[], line: number, start: number, end: number, symbols?: DocumentSymbols): string | null {
    const analysis = analyzeOperations(lines, symbols);
    const source = lines[line] ?? '';
    const at = (location: { line: number; column: number }, name: string) => {
        const column = location.column + (source[location.column - 1] === '@' ? 1 : 0);
        return location.line === line + 1 && start === column && end === column + name.length && source.slice(start - 1, end - 1) === name;
    };
    const context = analysis.contexts.get(line);
    const property = context?.operation?.inputs.find(input => at(input.location, input.name));
    if (property) return `**${property.name}** — ${typeReferenceText(property.type)}. Operation input, not a command request field. ${operationAvailability}`;
    const equals = source.indexOf('=');
    if (equals >= 0 && start - 1 > equals && context?.commandLine !== undefined && (context.operation || context.production?.declaration)) {
        const command = responseAnalysis(lines, symbols?.authoringDocuments ?? symbols?.authoringSources?.filter(text => text !== lines.join('\n')) ?? [], symbols?.authoringPlacement, symbols?.authoringPath).commands.get(context.commandLine);
        const expression = source.slice(equals + 1, end - 1).trim();
        const parts = expression.split('.');
        let properties: readonly { name: string; type: TypeReferenceSymbol }[] = command?.properties ?? [];
        for (const [index, part] of parts.entries()) {
            const property = properties.find(property => property.name === part);
            if (!property) break;
            if (index === parts.length - 1) return `**${part}** — ${typeReferenceText(property.type)}. Command source for an operation input. ${operationAvailability}`;
            const types = analysis.types.filter(type => type.name === property.type.name);
            if (types.length !== 1 || property.type.isCollection) break;
            properties = types[0].properties;
        }
    }
    const reference = operationReferenceAt(analysis, lines, line, start, end);
    if (reference?.declaration) return operationDetails(reference.declaration);
    if (reference?.kind === 'ambiguous') return `Ambiguous production: no declaration kind was selected. ${operationAvailability}`;
    const operation = context?.operation;
    if (operation && operation.location.line === line + 1) {
        const header = withoutComment(source).match(/^\s*(?:produces\s+)?operation\s+/)?.[0];
        if (header && start === header.length + 1 && end === start + operation.name.length) return operationDetails(operation);
    }
    if (operation?.usesLocation?.line === line + 1 && start >= operation.usesLocation.column && end <= operation.usesLocation.column + operation.uses.length && withoutComment(source).slice(start - 1, end - 1) === operation.uses.slice(start - operation.usesLocation.column, end - operation.usesLocation.column)) {
        const systems = analysis.systems.filter(system => system.name === operation.uses);
        return systems.length === 1 ? `**system ${systems[0].name}**\n\n${systems[0].description ?? ''}\n\n${operationAvailability}` : `Unresolved or ambiguous external system. ${operationAvailability}`;
    }
    for (const phase of [operation?.execute, operation?.compensate]) {
        if (!phase) continue;
        if (phase.location.line === line + 1 || phase.implementation?.location.line === line + 1 || phase.implementation?.hints.some(hint => hint.location.line === line + 1)) return `${phaseState(phase)}\n\n${phase.description ?? ''}\n\n${phase.implementation?.hints.map(hint => hint.text).join('\n') ?? ''}\n\n${operationAvailability}`;
    }
    const system = analysis.systems.find(system => system.location.path === (symbols?.authoringPath ?? 'current.play') && system.location.line === line + 1);
    return system ? `**system ${system.name}**\n\n${system.description ?? ''}\n\n${operationAvailability}` : null;
}
