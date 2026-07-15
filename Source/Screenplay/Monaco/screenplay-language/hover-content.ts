// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { enclosingChain, fenceMap, indentOf } from './document-context';
import { scanDocument } from './symbols';
import { getSubLanguage } from './sub-language-registry';
import { attributeDocs, contextVariableDocs, keywordDocs } from './keyword-docs';

// Produces the hover markdown for a word at a position, without any editor
// dependency — the Monaco service and the VSCode extension share this content.
// Columns are 1-based, matching both editors' word-at-position APIs.
export function hoverContent(
    lines: string[],
    lineIndex: number,
    word: string,
    startColumn: number,
    endColumn: number,
): string | null {
    const fences = fenceMap(lines);
    if (fences[lineIndex]) return null;

    const line = lines[lineIndex] ?? '';
    const before = line.charAt(startColumn - 2);

    if (before === '@') {
        const doc = attributeDocs[word];
        if (doc) return doc;
    }
    if (before === '$' || before === '.') {
        const variable = line.substring(0, endColumn - 1).match(/\$[\w.]*$/)?.[0];
        if (variable) {
            const doc =
                contextVariableDocs[variable] ??
                (variable.startsWith('$env') ? contextVariableDocs['$env'] : undefined);
            if (doc) return doc;
        }
    }

    const symbols = scanDocument(lines);

    const concept = symbols.concepts.find((candidate) => candidate.name === word);
    if (concept) {
        const attributes = concept.attributes.length ? ` ${concept.attributes.join(' ')}` : '';
        const values = concept.enumValues.length
            ? `\n\nValues: ${concept.enumValues.join(', ')}`
            : '';
        return `\`\`\`screenplay\nconcept ${concept.name} : ${concept.primitive}${attributes}\n\`\`\`${values}`;
    }

    const policy = symbols.policies.find((candidate) => candidate.name === word);
    if (policy) {
        const body = policy.requires.length ? policy.requires.join('\n') : '(custom C#)';
        return `\`\`\`screenplay\npolicy ${policy.name}\n${body}\n\`\`\``;
    }

    const event = symbols.events.find((candidate) => candidate.name === word);
    if (event) {
        const properties = event.properties
            .map((property) => `${property.name} ${property.type}`)
            .join('\n');
        return `\`\`\`screenplay\nevent ${event.name}\n${properties}\n\`\`\``;
    }

    // Sub-language keywords take precedence inside their construct.
    const chain = enclosingChain(lines, fences, lineIndex, indentOf(line));
    for (const construct of [line.trim().split(/\s+/)[0], ...chain]) {
        const subLanguage = getSubLanguage(construct ?? '');
        const doc = subLanguage?.hovers?.[word];
        if (doc) return doc;
    }

    const keywordDoc = keywordDocs[word];
    if (keywordDoc) return `**${word}** — ${keywordDoc}`;

    return null;
}
