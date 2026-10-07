// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { enclosingChain, fenceMap, indentOf, nearestEnclosingLine, withoutComment } from './document-context';
import { namedRuleContext } from './named-rule-context';
import { responseCompletions } from './response-completions';
import { exampleCompletions } from './example-authoring';
import { scanDocument } from './symbols';
import { getSubLanguage } from './sub-language-registry';
import * as items from './completion-items';
import { CompletionEntry } from './completion-items';

// Matches a validation rule line ending in "rule <Name>" (optionally followed by a
// message clause) - both the command form ("<property> rule <Name>") and the
// concept implied-subject form ("rule <Name>"). Its first word is the property
// name, not "rule", so it can't be recognized through the chain[0] keyword switch
// the way "on <EventType>" or "handler" are - hence the dedicated regex check.
const RULE_LINE_PATTERN = /(?:^|\s)rule\s+[A-Za-z_]\w*(?:\s+severity\s+(?:information|warning|error))?(?:\s+message\s+.*)?$/;

export type CompletionPlan =
    | { kind: 'none' }
    | { kind: 'entries'; entries: CompletionEntry[] }
    | { kind: 'contextVariables'; replaceLength: number }
    | { kind: 'playFiles'; replaceLength: number }
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
        case 'event':
            return items.eventItems;
        case 'system':
            return items.operationItems.filter(item => item.label === 'description');
        case 'operation':
            return items.operationItems;
        case 'execute':
        case 'compensate':
            return items.operationPhaseItems;
        case 'produces':
            return items.producesItems;
        case 'handler':
            return items.handlerItems;
        case 'implementation':
            return chain[1] === 'handler' ? items.implementationItems : ['execute', 'compensate'].includes(chain[1]) ? items.operationImplementationItems : [];
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
    if (fences[lineIndex] || withoutComment(textBefore).length < textBefore.length) return { kind: 'none' };
    const responseEntries = exampleCompletions(lines, lineIndex, textBefore) ?? responseCompletions(lines, lineIndex, textBefore, scanDocument(lines));
    if (responseEntries !== null) return { kind: 'entries', entries: responseEntries };

    // Inside the quotes of a file import, what is wanted is a path - replacing what has been typed so far.
    const importPath = textBefore.match(/^\s*import\s+"([^"]*)$/);
    if (importPath) {
        return { kind: 'playFiles', replaceLength: importPath[1].length };
    }

    // A quote or a slash only asks for a completion inside an import path.
    if (/["/]$/.test(textBefore)) return { kind: 'none' };

    const contextVariableMatch = textBefore.match(/\$[\w.]*$/);
    if (contextVariableMatch) {
        return { kind: 'contextVariables', replaceLength: contextVariableMatch[0].length };
    }

    const currentLine = lines[lineIndex] ?? '';
    const effectiveIndent =
        textBefore.trim().length === 0 ? textBefore.length : indentOf(currentLine);
    const chain = enclosingChain(lines, fences, lineIndex, effectiveIndent);
    const ruleContext = namedRuleContext(lines, lineIndex, effectiveIndent);
    if (ruleContext === 'implementation') return { kind: 'entries', entries: items.namedRuleImplementationItems };
    if (ruleContext === 'rule') return { kind: 'entries', entries: items.commandRuleItems };

    const propertyOwner = ['event', 'command', 'type', 'readmodel', 'trigger', 'when', 'every', 'at'].includes(chain[0]) ||
        (chain[0] === 'produces' && /^produces\s+event\b/.test(nearestEnclosingLine(lines, fences, lineIndex, effectiveIndent) ?? ''));
    const propertyName = textBefore.trimStart().split(/\s+/)[0];
    const reservedProperty = (chain[0] === 'command' && ['reads', 'authorize', 'produces'].includes(propertyName)) ||
        (chain[0] === 'event' && propertyName === 'tag') ||
        (['trigger', 'when', 'every', 'at'].includes(chain[0]) && ['description', 'file', 'reads', 'produces', 'invokes'].includes(propertyName));
    const optionalPrefix = (match: RegExpMatchArray | null) => match !== null && 'optional'.startsWith(match[1]);
    const afterPropertyType = propertyOwner && !reservedProperty && optionalPrefix(textBefore.match(/^\s*@?[a-z_]\w*\s+[\w.]+(?:\[\])?\s+(\w*)$/));
    const afterQueryType = (chain[0] === 'query' && optionalPrefix(textBefore.match(/^\s*(?:by|filter)\s+[a-z_]\w*\s+[\w.]+(?:\[\])?\s+(\w*)$/))) ||
        optionalPrefix(textBefore.match(/^\s*query\s+[A-Za-z_]\w*\s*=>\s*(?:observable\s+)?[\w.]+(?:\[\])?\s+(\w*)$/));
    if ((afterPropertyType || afterQueryType) && !/=>\s*observable\s+$/.test(textBefore)) return { kind: 'entries', entries: items.optionalTypeItems };

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
    if (chain[0] === 'specification') {
        if (/^\s*when\s+query\s+[\w.]*$/.test(textBefore)) return { kind: 'queries' };
        const step = textBefore.match(/^\s*(given|when|then)\s+\w*$/);
        if (step) return { kind: 'entries', entries: items.specificationStepItems[step[1] as 'given' | 'when' | 'then'] };
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
    if (enclosingLine && /^produces\s+operation\b/.test(enclosingLine)) return { kind: 'entries', entries: items.operationItems };
    if (enclosingLine && /^produces\s+event\b/.test(enclosingLine)) {
        if (/^\s+@?[a-z_]\w*\s+[\w[\]?]*$/.test(textBefore)) return { kind: 'types' };
        return { kind: 'entries', entries: items.inlineEventItems };
    }
    if (enclosingLine && RULE_LINE_PATTERN.test(enclosingLine)) {
        return { kind: 'entries', entries: items.ruleItems };
    }

    return { kind: 'entries', entries: completionEntriesFor(chain) };
}
