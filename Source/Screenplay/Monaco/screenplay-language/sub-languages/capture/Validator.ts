// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationIssue } from '../../validation';

interface MeaningfulLine {
    line: string;
    trimmed: string;
    lineNumber: number;
    indent: number;
}

// Structural rules only, unaffected by the source-property grammar changes — the
// grammar's own syntax (see grammar.md) is what the tokenizer enforces; these are
// the compiler's semantic constraints on top of it. Takes plain lines rather than
// a Monaco model so it stays usable outside an editor, matching ../../validation.ts.
export class Validator {
    validate(lines: string[]): ValidationIssue[] {
        const issues: ValidationIssue[] = [];

        let firstNonEmptyLineIndex = -1;
        let firstLine = '';
        for (let index = 0; index < lines.length; index++) {
            const trimmed = lines[index].trim();
            if (trimmed && !trimmed.startsWith('#')) {
                firstNonEmptyLineIndex = index;
                firstLine = trimmed;
                break;
            }
        }

        if (!firstLine) {
            return issues;
        }

        const captureMatch = firstLine.match(/^capture\s+([\w.]+)\s*$/);
        if (!captureMatch) {
            issues.push(this.error(firstNonEmptyLineIndex, 1, firstLine.length + 1, 'Capture definition must start with "capture <Name>"'));
            return issues;
        }

        const meaningfulLines: MeaningfulLine[] = lines
            .map((line, index) => ({
                line,
                trimmed: line.trim(),
                lineNumber: index,
                indent: line.search(/\S/),
            }))
            .filter((entry) => entry.trimmed && !entry.trimmed.startsWith('#'));

        const hasSource = meaningfulLines.some((entry) => entry.trimmed.startsWith('source '));
        if (!hasSource) {
            issues.push(this.error(firstNonEmptyLineIndex, 1, firstLine.length + 1, 'Capture definition must include a "source" block'));
        }

        const hasKey = meaningfulLines.some((entry) => entry.trimmed.startsWith('key '));
        if (!hasKey) {
            issues.push(this.error(firstNonEmptyLineIndex, 1, firstLine.length + 1, 'Capture definition must include a "key" declaration'));
        }

        for (let index = 0; index < meaningfulLines.length; index++) {
            const current = meaningfulLines[index];
            if (!current.trimmed.startsWith('append ')) {
                continue;
            }

            let hasWhenClause = false;
            for (let next = index + 1; next < meaningfulLines.length; next++) {
                const candidate = meaningfulLines[next];
                if (candidate.indent <= current.indent) {
                    break;
                }
                if (candidate.trimmed.startsWith('when ')) {
                    hasWhenClause = true;
                    break;
                }
            }

            if (!hasWhenClause) {
                const eventName = current.trimmed.substring('append '.length).trim() || 'unnamed append block';
                const startColumn = current.line.indexOf(eventName) + 1;
                issues.push(this.error(
                    current.lineNumber,
                    startColumn > 0 ? startColumn : 1,
                    startColumn > 0 ? startColumn + eventName.length : current.line.length + 1,
                    `Append block '${eventName}' must include a "when" clause`,
                ));
            }
        }

        return issues;
    }

    private error(line: number, startColumn: number, endColumn: number, message: string): ValidationIssue {
        return { severity: 'error', line, startColumn, endColumn, message };
    }
}
