// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { causedByProperties, contextRoots, identityProperties, primitiveTypes, sliceTypes } from './language';
import { fenceMap, indentOf } from './document-context';
import {
    DocumentSymbols,
    PropertySymbol,
    knownEventNames,
    knownTypeNames,
    scanDocument,
} from './symbols';

export type ValidationSeverity = 'error' | 'warning';

export interface ValidationIssue {
    line: number;
    startColumn: number;
    endColumn: number;
    message: string;
    severity: ValidationSeverity;
}

function issue(
    severity: ValidationSeverity,
    line: number,
    startColumn: number,
    length: number,
    message: string,
): ValidationIssue {
    return { severity, message, line, startColumn, endColumn: startColumn + length };
}

function tokenIssue(
    severity: ValidationSeverity,
    line: number,
    text: string,
    token: string,
    message: string,
): ValidationIssue {
    const column = text.indexOf(token) + 1;
    return issue(severity, line, column, token.length, message);
}

// Reports every property whose type reference names nothing the document declares, and
// every command that marks more than one property as its identifier.
function validateDeclarations(lines: string[], symbols: DocumentSymbols): ValidationIssue[] {
    const issues: ValidationIssue[] = [];
    const types = new Set(knownTypeNames(symbols, primitiveTypes));

    const checkProperties = (properties: PropertySymbol[], owner: string) => {
        for (const property of properties.filter(
            (candidate) => !types.has(candidate.type.replace(/[[\]?]/g, '')),
        )) {
            const bare = property.type.replace(/[[\]?]/g, '');
            issues.push(
                tokenIssue(
                    'warning',
                    property.line,
                    lines[property.line],
                    property.type,
                    `Unknown type '${bare}' on '${property.name}' of ${owner} — declare it with 'concept ${bare} : <Primitive>' or 'type ${bare}'.`,
                ),
            );
        }
    };

    for (const type of symbols.types) checkProperties(type.properties, `type '${type.name}'`);
    for (const event of symbols.events) checkProperties(event.properties, `event '${event.name}'`);
    for (const query of symbols.queries) checkProperties(query.parameters, `query '${query.name}'`);
    for (const command of symbols.commands) {
        checkProperties(command.properties, `command '${command.name}'`);
        const identifiers = command.properties.filter((property) => property.isIdentifier);
        for (const extra of identifiers.slice(1)) {
            issues.push(
                tokenIssue(
                    'error',
                    extra.line,
                    lines[extra.line],
                    'identifier',
                    `Command '${command.name}' already marks '${identifiers[0].name}' as identifier — only one property can be the identifier.`,
                ),
            );
        }
    }

    const declared = new Map<string, number>();
    for (const declaration of [...symbols.concepts, ...symbols.types]) {
        const first = declared.get(declaration.name);
        if (first === undefined) {
            declared.set(declaration.name, declaration.line);
            continue;
        }
        issues.push(
            tokenIssue(
                'error',
                declaration.line,
                lines[declaration.line],
                declaration.name,
                `Duplicate declaration of '${declaration.name}' — concept and type names must be unique.`,
            ),
        );
    }

    return issues;
}

// Validates a Screenplay document without any editor dependency — both the Monaco
// service and the VSCode extension adapt these issues to their marker/diagnostic APIs.
export function validateLines(lines: string[]): ValidationIssue[] {
    const fences = fenceMap(lines);
    const symbols = scanDocument(lines);
    const events = new Set(knownEventNames(symbols));
    const policies = new Set(symbols.policies.map((policy) => policy.name));
    const issues: ValidationIssue[] = validateDeclarations(lines, symbols);

    const checkEvent = (line: number, text: string, name: string) => {
        if (!events.has(name)) {
            issues.push(
                tokenIssue('warning', line, text, name, `Unknown event type '${name}' — declare it in a slice or import it.`),
            );
        }
    };

    let authorizeIndent = -1;

    for (let index = 0; index < lines.length; index++) {
        const line = lines[index];
        const trimmed = line.trim();

        if (fences[index] || trimmed.length === 0) continue;

        const leading = line.match(/^[ ]*(\t+)/);
        if (leading) {
            issues.push(
                issue('warning', index, leading[0].length - leading[1].length + 1, leading[1].length, 'Screenplay is indentation-based — use spaces, not tabs.'),
            );
        }

        // Policy references continue on indented lines below an authorize clause.
        if (authorizeIndent >= 0) {
            if (
                indentOf(line) > authorizeIndent &&
                /^(?:or\s+)?[A-Z]\w*(?:\s+or\s+[A-Z]\w*)*$/.test(trimmed)
            ) {
                for (const name of trimmed.split(/\s+/).filter((token) => token !== 'or')) {
                    if (!policies.has(name)) {
                        issues.push(tokenIssue('warning', index, line, name, `Unknown policy '${name}' — declare it with 'policy ${name}'.`));
                    }
                }
                continue;
            }
            authorizeIndent = -1;
        }

        const slice = trimmed.match(/^slice\s+(\w+)/);
        if (slice && !sliceTypes.includes(slice[1])) {
            issues.push(
                tokenIssue('error', index, line, slice[1], `Unknown slice type '${slice[1]}' — expected ${sliceTypes.join(', ')}.`),
            );
        }

        const concept = trimmed.match(/^concept\s+\w+\s*:\s*(\w+)/);
        if (concept && concept[1] !== 'Enum' && !primitiveTypes.includes(concept[1])) {
            issues.push(
                tokenIssue('error', index, line, concept[1], `Unknown primitive type '${concept[1]}' — expected ${primitiveTypes.join(', ')} or Enum.`),
            );
        }

        const authorize = trimmed.match(/^authorize\s+(.*)$/);
        if (authorize) {
            authorizeIndent = indentOf(line);
            for (const name of authorize[1].split(/\s+/).filter((token) => token !== 'or' && token.length > 0)) {
                if (!policies.has(name)) {
                    issues.push(tokenIssue('warning', index, line, name, `Unknown policy '${name}' — declare it with 'policy ${name}'.`));
                }
            }
        }

        const reactsOn = trimmed.match(/^on\s+([A-Z]\w*)\s*$/);
        if (reactsOn) checkEvent(index, line, reactsOn[1]);

        const produces = trimmed.match(/^produces\s+([A-Z]\w*)\s*$/);
        if (produces) checkEvent(index, line, produces[1]);

        if (/^produces\s+when\b/.test(trimmed)) {
            for (let next = index + 1; next < lines.length; next++) {
                const candidate = lines[next];
                if (fences[next] || candidate.trim().length === 0) continue;
                if (indentOf(candidate) > indentOf(line)) {
                    const name = candidate.trim().match(/^([A-Z]\w*)\s*$/);
                    if (name) checkEvent(next, candidate, name[1]);
                }
                break;
            }
        }

        const uniqueEvent = trimmed.match(/^unique\s+event\s+([A-Z]\w*)/);
        if (uniqueEvent) checkEvent(index, line, uniqueEvent[1]);

        const uniqueProperty = trimmed.match(/^unique\s+[a-z_]\w*\s+on\s+([A-Z]\w*)/);
        if (uniqueProperty) checkEvent(index, line, uniqueProperty[1]);

        // Every $context. path must name something CommandContext or QueryContext carries.
        for (const path of line.match(/\$context\.[\w.]+/g) ?? []) {
            const segments = path.substring('$context.'.length).split('.');
            if (!contextRoots.includes(segments[0])) {
                issues.push(
                    tokenIssue('warning', index, line, path, `Unknown $context path '${segments.join('.')}' — expected one of ${contextRoots.join(', ')}.`),
                );
            } else if (
                segments[0] === 'causedBy' &&
                segments.length > 1 &&
                !causedByProperties.includes(segments[1])
            ) {
                issues.push(
                    tokenIssue('warning', index, line, path, `Unknown $context.causedBy property '${segments[1]}' — expected ${causedByProperties.join(', ')}.`),
                );
            } else if (
                segments[0] === 'identity' &&
                segments.length > 1 &&
                !identityProperties.includes(segments[1])
            ) {
                issues.push(
                    tokenIssue('warning', index, line, path, `Unknown $context.identity property '${segments[1]}' — expected ${identityProperties.join(', ')}.`),
                );
            }
        }
    }

    const fenceLines = lines
        .map((line, index) => ({ line, index }))
        .filter(({ line }) => /^\s*```\s*$/.test(line));
    if (fenceLines.length % 2 === 1) {
        const last = fenceLines[fenceLines.length - 1];
        issues.push(issue('error', last.index, 1, last.line.length + 1, 'Unclosed inline code block — expected a closing ``` line.'));
    }

    return issues;
}
