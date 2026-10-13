// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pattern } from '@cratis/screenplay-compiler';
import { responseAnalysis } from './response-analysis';
import { DocumentSymbols } from './symbols';
import { CompletionEntry } from './completion-items';
import { typeReferenceText } from './TypeReferenceSymbol';

function fieldsAt(lines: string[], line: number, symbols?: DocumentSymbols) {
    return responseAnalysis(lines, symbols?.authoringDocuments ?? symbols?.authoringSources, symbols?.authoringPlacement, symbols?.authoringPath, symbols?.authoringPlacementResolved).continuationResponses?.get(line) ?? [];
}

export function continuationResponseCompletions(lines: string[], line: number, before: string, symbols: DocumentSymbols): CompletionEntry[] | null {
    if (!pattern(/^\s*(?:with\s+\w+\s+from\s+|on\s+submit\s+navigate\s+to\s+[\w.]+\s+by\s+)\w*$/.source).test(before)) return null;
    const fields = fieldsAt(lines, line, symbols);
    return fields.length === 0 ? null : fields.map(field => ({ label: field.name, insertText: field.name, detail: typeReferenceText(field.type), sortText: `0_${field.name}`, documentation: `Returned by ${field.command} on success` }));
}

export function continuationResponseHover(lines: string[], line: number, word: string, startColumn: number, endColumn: number, symbols?: DocumentSymbols): string | null {
    const content = lines[line] ?? '';
    const operand = pattern(/^(\s*(?:with\s+\w+\s+from\s+|on\s+submit\s+navigate\s+to\s+[\w.]+\s+by\s+|set\s+[\w.$]+\s+to\s+|notify\s+(?:info|warning|error)\s+|confirm\s+))([A-Za-z_]\w*)\s*(?:\/\/.*)?$/.source).exec(content);
    if (!operand || operand[2] !== word || startColumn !== operand[1].length + 1 || endColumn !== startColumn + word.length) return null;
    const field = fieldsAt(lines, line, symbols).find(field => field.name === word);
    return field ? `**${field.name}** — ${typeReferenceText(field.type)}. Returned by ${field.command} on success.` : null;
}
