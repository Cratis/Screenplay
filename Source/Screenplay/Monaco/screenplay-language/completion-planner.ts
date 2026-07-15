// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { enclosingChain, fenceMap, indentOf } from './document-context';
import { getSubLanguage } from './sub-language-registry';
import * as items from './completion-items';
import { CompletionEntry } from './completion-items';

export type CompletionPlan =
    | { kind: 'none' }
    | { kind: 'entries'; entries: CompletionEntry[] }
    | { kind: 'contextVariables'; replaceLength: number }
    | { kind: 'policies' }
    | { kind: 'events' }
    | { kind: 'producesTargets' }
    | { kind: 'screens' }
    | { kind: 'queries' }
    | { kind: 'types' };

export function completionEntriesFor(chain: string[]): CompletionEntry[] {
    const construct = chain[0];
    if (construct === undefined) return items.topLevelItems;
    const subLanguage = getSubLanguage(construct);
    if (subLanguage) return subLanguage.completions ?? [];
    switch (construct) {
        case 'module':
            return items.moduleItems;
        case 'feature':
            return items.featureItems;
        case 'slice':
            return items.sliceItems;
        case 'command':
            return items.commandItems;
        case 'produces':
            return items.producesItems;
        case 'query':
            return items.queryItems;
        case 'constraint':
            return items.constraintItems;
        case 'reactor':
            return items.reactorItems;
        case 'on':
            return items.reactorOnItems;
        case 'policy':
            return items.policyItems;
        case 'validate':
            return items.validateItems;
        case 'action':
            return items.actionItems;
        case 'table':
        case 'summary':
            return items.tableItems;
        default:
            // Layout slots and sections inside a screen expose the screen vocabulary.
            if (chain.includes('screen') || construct === 'section' || construct === 'layout') {
                return items.screenItems;
            }
            if (chain.includes('produces')) return items.producesItems;
            return [];
    }
}

// Decides what to complete at a position, without any editor dependency — the
// Monaco service and the VSCode extension both materialize the plan into items.
export function planCompletions(
    lines: string[],
    lineIndex: number,
    textBefore: string,
): CompletionPlan {
    const fences = fenceMap(lines);
    if (fences[lineIndex]) return { kind: 'none' };

    const contextVariableMatch = textBefore.match(/\$[\w.]*$/);
    if (contextVariableMatch) {
        return { kind: 'contextVariables', replaceLength: contextVariableMatch[0].length };
    }

    const currentLine = lines[lineIndex] ?? '';
    const effectiveIndent =
        textBefore.trim().length === 0 ? textBefore.length : indentOf(currentLine);
    const chain = enclosingChain(lines, fences, lineIndex, effectiveIndent);

    if (/\bauthorize\s+[\w\s]*$/.test(textBefore) || chain[0] === 'authorize') {
        return { kind: 'policies' };
    }
    if (/\bon\s+\w*$/.test(textBefore) && (chain.includes('reactor') || chain.includes('constraint'))) {
        return { kind: 'events' };
    }
    if (/\b(?:on|unique\s+event)\s+\w*$/.test(textBefore) && chain[0] === 'constraint') {
        return { kind: 'events' };
    }
    if (/\bproduces\s+\w*$/.test(textBefore)) {
        return { kind: 'producesTargets' };
    }
    if (/\bnavigate\s+to\s+\w*$/.test(textBefore)) {
        return { kind: 'screens' };
    }
    if (/\bvia\s+query\s+\w*$/.test(textBefore)) {
        return { kind: 'queries' };
    }
    if (
        (chain[0] === 'event' || chain[0] === 'command') &&
        /^\s+[a-z_]\w*\s+[\w[\]?]*$/.test(textBefore)
    ) {
        return { kind: 'types' };
    }

    return { kind: 'entries', entries: completionEntriesFor(chain) };
}
