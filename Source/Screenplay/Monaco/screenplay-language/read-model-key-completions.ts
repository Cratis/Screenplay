// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { responseAnalysis } from './response-analysis';
import { DocumentSymbols } from './symbols';
import { CompletionEntry } from './completion-items';
import { enclosingHeaders, fenceMap, indentOf } from './document-context';

export function readModelKeyCompletions(lines: string[], line: number, before: string, symbols: DocumentSymbols): CompletionEntry[] | null {
    const headers = enclosingHeaders(lines, fenceMap(lines), line, indentOf(before));
    if (headers[0] !== 'by') return null;
    const owner = headers[1] ?? '';
    const read = owner.match(/^reads\s+([\w.]+)/);
    const query = owner.match(/^query\s+\w+\s*=>\s*(?:observable\s+)?([\w.]+)/);
    if (!read && !query) return null;
    const analysis = responseAnalysis(lines, symbols.authoringDocuments ?? symbols.authoringSources ?? [], symbols.authoringPlacement, symbols.authoringPath, symbols.authoringPlacementResolved);
    const name = read?.[1] ?? query?.[1];
    const models = analysis.readModels.filter(({ model, scope }) => model.name === name || [...scope, model.name].join('.') === name);
    if (models.length !== 1) return [];
    const parts = models[0].model.properties.filter(property => property.isKey);
    return parts.map(part => ({ label: part.name, insertText: read ? `${part.name} = \${1:${part.name}}` : `${part.name} ${part.type.name}`, documentation: 'Supply this read-model key part by name. Composite keys are authoring-only (PLAY0268, #599).' }));
}
