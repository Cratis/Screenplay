// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { primitiveTypes, sliceTypes } from './language';
import { fenceMap, indentOf } from './document-context';
import { knownEventNames, scanDocument } from './symbols';

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

// Validates a Screenplay document without any editor dependency — both the Monaco
// service and the VSCode extension adapt these issues to their marker/diagnostic APIs.
export function validateLines(lines: string[]): ValidationIssue[] {
    const fences = fenceMap(lines);
    const symbols = scanDocument(lines);
    const events = new Set(knownEventNames(symbols));
    const policies = new Set(symbols.policies.map((policy) => policy.name));
    const issues: ValidationIssue[] = [];

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
