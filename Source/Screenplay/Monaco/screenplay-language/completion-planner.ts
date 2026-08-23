// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { enclosingChain, fenceMap, indentOf, nearestEnclosingLine } from './document-context';
import { getSubLanguage } from './sub-language-registry';
import * as items from './completion-items';
import { CompletionEntry } from './completion-items';

// Matches a validation rule line ending in "rule <Name>" (optionally followed by a
// message clause) - both the command form ("<property> rule <Name>") and the
// concept implied-subject form ("rule <Name>"). Its first word is the property
// name, not "rule", so it can't be recognized through the chain[0] keyword switch
// the way "on <EventType>" or "handler" are - hence the dedicated regex check.
const RULE_LINE_PATTERN = /(?:^|\s)rule\s+[A-Za-z_]\w*(?:\s+message\s+.*)?$/;

export type CompletionPlan =
    | { kind: 'none' }
    | { kind: 'entries'; entries: CompletionEntry[] }
    | { kind: 'contextVariables'; replaceLength: number }
    | { kind: 'policies' }
    | { kind: 'events' }
    | { kind: 'triggers' }
    | { kind: 'commands' }
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
        case 'concept':
            return items.conceptItems;
        case 'type':
            return items.typeItems;
        case 'command':
            return items.commandItems;
        case 'produces':
            return items.producesItems;
        case 'handler':
            return items.handlerItems;
        case 'query':
            return items.queryItems;
        case 'performer':
            return items.performerItems;
        case 'constraint':
            return items.constraintItems;
        case 'reaction':
            return items.reactionItems;
        case 'trigger':
            return items.triggerItems;
        case 'when':
        case 'every':
        case 'at':
            return chain.includes('reaction') ? items.reactionTriggerItems : [];
        case 'specification':
            return items.specificationItems;
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
    if (/\bwhen\s+\w*$/.test(textBefore) && chain.includes('reaction')) {
        return { kind: 'triggers' };
    }
    if (/\bon\s+\w*$/.test(textBefore) && chain.includes('constraint')) {
        return { kind: 'events' };
    }
    if (/\binvokes\s+\w*$/.test(textBefore) && chain.includes('reaction')) {
        return { kind: 'commands' };
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
    if (/\b(?:via|then)\s+query\s+[\w.]*$/.test(textBefore)) {
        return { kind: 'queries' };
    }
    if (
        (chain[0] === 'event' || chain[0] === 'command' || chain[0] === 'type') &&
        /^\s+[a-z_]\w*\s+[\w[\]?]*$/.test(textBefore)
    ) {
        return { kind: 'types' };
    }
    if (chain[0] === 'query' && /^\s+(?:by|filter)\s+[a-z_]\w*\s+[\w[\]?]*$/.test(textBefore)) {
        return { kind: 'types' };
    }

    const enclosingLine = nearestEnclosingLine(lines, fences, lineIndex, effectiveIndent);
    if (enclosingLine && RULE_LINE_PATTERN.test(enclosingLine)) {
        return { kind: 'entries', entries: items.ruleItems };
    }

    return { kind: 'entries', entries: completionEntriesFor(chain) };
}
