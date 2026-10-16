// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompletionEntry } from './completion-items';
import { enclosingChain, fenceMap, indentOf } from './document-context';
import { responseAnalysis } from './response-analysis';
import { DocumentSymbols, scanDocument } from './symbols';

export function commandReadCompletions(lines: string[], line: number, before: string, symbols: DocumentSymbols): CompletionEntry[] | null {
    const sources = [lines.join('\n'), ...(symbols.authoringDocuments?.map(document => document.source) ?? symbols.authoringSources ?? [])];
    const protectedMode = sources.some(source => {
        const document = source.split('\n');
        const fences = fenceMap(document);
        return document.some((text, index) => !fences[index] && /^command\s+reads\s+protected\s*(?:\/\/.*)?$/.test(text));
    });
    if (!protectedMode) return null;
    const chain = enclosingChain(lines, fenceMap(lines), line, indentOf(before));
    if (!chain.includes('command')) return null;
    const operand = before.match(/^\s*require\s+(?:.*(?:==|!=|>=|<=|>|<|\band\b|\bor\b)\s+)?([\w.]*)$/);
    const mapping = chain[0] === 'produces' ? before.match(/^\s*\w+\s*=\s*([\w.]*)$/) : null;
    const prefix = (operand ?? mapping)?.[1];
    if (prefix === undefined) return null;
    const analysis = responseAnalysis(lines, symbols.authoringDocuments ?? symbols.authoringSources?.filter(source => source !== lines.join('\n')) ?? [], symbols.authoringPlacement, symbols.authoringPath, symbols.authoringPlacementResolved);
    const commandLine = analysis.operations.contexts.get(line)?.commandLine;
    const command = commandLine === undefined ? undefined : scanDocument(lines).commands.find(command => command.line === commandLine);
    if (!command) return [];
    const reads = command.reads ?? [];
    return reads.flatMap(read => {
        const name = read.alias ?? read.view;
        if (reads.filter(other => (other.alias ?? other.view) === name).length !== 1 || command.properties.some(property => property.name === name)) return [];
        if (!read.alias && reads.filter(other => other.view === read.view).length !== 1) return [];
        const models = analysis.readModels.filter(({ model, scope }) => model.name === read.view || [...scope, model.name].join('.') === read.view);
        if (models.length !== 1) return [];
        return models[0].model.properties.filter(property => `${name}.${property.name}`.startsWith(prefix)).map(property => ({
            label: `${name}.${property.name}`,
            insertText: prefix.includes('.') ? property.name : `${name}.${property.name}`,
            documentation: 'Protected read property in require or produces mappings (ESM v25). Required absence rejects before requirements; matching intervening facts conflict at commit. Admission depends on the view and key shape.',
        }));
    });
}
