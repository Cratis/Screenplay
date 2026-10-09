// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { enclosingChain, enclosingHeaders, fenceMap, indentOf, nearestEnclosingLine, withoutComment } from './document-context';
import { refusalContext } from './refusal-context';
import { namedRuleContext } from './named-rule-context';
import { responseCompletions } from './response-completions';
import { dependencyTargetCompletions } from './dependency-completions';
import { exampleCompletions } from './example-authoring';
import { DocumentSymbols, scanDocument } from './symbols';
import { structureCompletion } from './structure-completions';
import { getSubLanguage } from './sub-language-registry';
import * as items from './completion-items';
import * as scope from './scope-items';
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

// What the position sits inside, beyond the keyword chain: the whole header of each enclosing block, so a
// slice's type and a template's kind are known, and the document's own lines, so a declaration that may
// appear once is not offered again.
export interface CompletionScope {
    headers: readonly string[];
    document: readonly string[];
}

const declaredIn = (document: readonly string[], keyword: string) => document.some(line => new RegExp(`^${keyword}\\b`).test(line));

function topLevelEntries(document: readonly string[]): CompletionEntry[] {
    return items.topLevelItems.filter(item => !(item.label === 'domain' && declaredIn(document, 'domain')) && !(item.label === 'authentication' && declaredIn(document, 'authentication')));
}

const isTemplate = (header: string | undefined) => /^(?:screen|dialog)\s+template\b/.test(header ?? '');

// A whole block as one completion entry. Continuation lines are indented relative to the line it is inserted on,
// which is how the editors indent a multi-line snippet.
function expansionEntry(text: string, indent: number): CompletionEntry {
    const relative = text.split('\n').map((line, index) => (index === 0 ? line : line.slice(Math.min(indent, indentOf(line))))).join('\n');
    const label = text.split('\n')[0].trim();
    return { label, insertText: relative.replace(/[$}\\]/g, '\\$&'), documentation: 'Expands the whole block from what the command and its events declare.' };
}

export function completionEntriesFor(chain: string[], where: CompletionScope = { headers: [], document: [] }): CompletionEntry[] {
    const construct = chain[0];
    if (construct === undefined || construct === 'domain') return topLevelEntries(where.document);
    const subLanguage = getSubLanguage(construct);
    if (subLanguage) return subLanguage.completions ?? [];
    const header = where.headers[0];
    switch (construct) {
        case 'module':
            return items.moduleItems;
        case 'feature':
            return items.featureItems;
        case 'slice':
            return items.sliceItemsFor(header?.split(/\s+/)[1]);
        case 'concept':
            return items.conceptItems;
        case 'type':
            return items.typeItems;
        case 'command':
            return items.commandItems;
        case 'event':
            return items.eventItems;
        case 'readmodel':
            return scope.readModelItems;
        case 'reducer':
            return scope.reducerItems;
        case 'form':
            return scope.formItems;
        case 'contribute':
            return scope.contributeItems;
        case 'behavior':
            return scope.behaviorItems;
        case 'persona':
            return scope.personaItems;
        case 'authentication':
            return scope.authenticationItems;
        case 'seed':
            return scope.seedItems;
        case 'theme':
            return scope.themeItems;
        case 'ui':
            return scope.uiProfileItems;
        case 'on':
        case 'success':
        case 'failure':
        case 'confirm':
            return scope.outcomeItems;
        case 'layout':
            return scope.layoutItems;
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
        case 'invokes':
            return chain.includes('reaction') ? items.invocationItems : [];
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
        case 'screen':
            return isTemplate(header) ? scope.templateItems : items.screenItems;
        case 'dialog':
            return scope.templateItems;
        default:
            if (isTemplate(where.headers.find(candidate => isTemplate(candidate)))) return scope.templateItems;
            // Sections inside a screen expose the screen vocabulary.
            if (chain.includes('screen') || construct === 'section') return items.screenItems;
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
    symbols: DocumentSymbols = scanDocument(lines),
): CompletionPlan {
    const fences = fenceMap(lines);
    if (fences[lineIndex] || withoutComment(textBefore).length < textBefore.length) return { kind: 'none' };
    if (/^\s*concept\s+[\p{L}\p{Mn}\p{Nd}\p{Pc}]+\s*:\s*\w+\s+(?:(?:pii|personal|secret)\s+)*\w*$/u.test(textBefore)) {
        return { kind: 'entries', entries: ['pii', 'secret'].filter(marker => !new RegExp(`\\b${marker}\\b`).test(textBefore)).map(marker => ({ label: marker, insertText: marker, documentation: marker === 'pii' ? 'personal data (GDPR Art. 4(1)); renders Chronicle [PII]' : 'Operational secret; renders [Encrypted] + [NotAudited].' })) };
    }
    const inConcept = enclosingChain(lines, fences, lineIndex, indentOf(textBefore))[0] === 'concept';
    if (inConcept && /^\s*(?:secret|sensitive|@sensitive)\s+scope\s+\w*$/.test(textBefore)) {
        return { kind: 'entries', entries: ['subject', 'namespace', 'global'].map(scope => ({ label: scope, insertText: scope, documentation: 'Explicit secret encryption scope.' })) };
    }
    if (inConcept && /^\s*(?:pii|personal|@pii)\s+special\s+\w*$/.test(textBefore)) {
        return { kind: 'entries', entries: ['racialOrEthnicOrigin', 'politicalOpinions', 'religiousOrPhilosophicalBeliefs', 'tradeUnionMembership', 'genetic', 'biometric', 'health', 'sexLifeOrSexualOrientation'].map(category => ({ label: category, insertText: category, documentation: 'GDPR Art. 9(1) special category.' })) };
    }
    const responseEntries = exampleCompletions(lines, lineIndex, textBefore) ?? responseCompletions(lines, lineIndex, textBefore, scanDocument(lines));
    if (responseEntries !== null) return { kind: 'entries', entries: responseEntries };
    const dependencyEntries = dependencyTargetCompletions(lines, lineIndex, textBefore, symbols);
    if (dependencyEntries !== null) return { kind: 'entries', entries: dependencyEntries };

    // Inside the quotes of a file import, what is wanted is a path - replacing what has been typed so far.
    const importPath = textBefore.match(/^\s*import\s+"([^"]*)$/);
    if (importPath) {
        return { kind: 'playFiles', replaceLength: importPath[1].length };
    }

    // A quote or a slash only asks for a completion inside an import path.
    if (/["/]$/.test(textBefore)) return { kind: 'none' };

    const refusal = refusalContext(lines, lineIndex, indentOf(lines[lineIndex] ?? textBefore));
    if (/\$refusal\.\w*$/.test(textBefore)) {
        return { kind: 'entries', entries: refusal === undefined ? [] : ['reason', 'message', ...(/^on\s+refused\s+by\s+constraint(?:\s|$)/.test(refusal) ? ['constraint'] : [])].map(member => ({ label: member, insertText: member, documentation: 'String refusal value; syntax-only, not yet executable (PLAY0268).' })) };
    }
    const contextVariableMatch = textBefore.match(/\$[\w.]*$/);
    if (contextVariableMatch) {
        return { kind: 'contextVariables', replaceLength: contextVariableMatch[0].length };
    }

    const currentLine = lines[lineIndex] ?? '';
    const effectiveIndent =
        textBefore.trim().length === 0 ? textBefore.length : indentOf(currentLine);
    const chain = enclosingChain(lines, fences, lineIndex, effectiveIndent);
    if (chain.includes('screen')) {
        let parentIndent = effectiveIndent;
        let guarded = false;
        for (let index = lineIndex - 1; index >= 0; index--) {
            if (fences[index]) continue;
            const parent = withoutComment(lines[index]);
            if (parent.trim().length === 0 || indentOf(parent) >= parentIndent) continue;
            parentIndent = indentOf(parent);
            if (/^\s*action\s+(?:"|\$strings\.)/.test(parent)) { guarded = true; break; }
            if (/^\s*screen\b/.test(parent)) break;
        }
        if (guarded) {
            if (/^\s*(?:when\b.*|otherwise)\s+execute\s+[\w.]*$/.test(textBefore)) return { kind: 'commands' };
            if (chain[0] === 'action') return { kind: 'entries', entries: items.guardedActionItems };
            if (['when', 'otherwise'].includes(chain[0])) {
                const parent = nearestEnclosingLine(lines, fences, lineIndex, effectiveIndent) ?? '';
                return { kind: 'entries', entries: /\bexecute\s+[\w.]+$/.test(parent) ? items.actionArgumentItems : [] };
            }
        }
    }
    if (['on', 'when', 'otherwise'].includes(chain[0])) {
        let parentIndent = effectiveIndent;
        for (let index = lineIndex - 1; index >= 0; index--) {
            if (fences[index]) continue;
            const parent = withoutComment(lines[index]);
            if (parent.trim().length === 0 || indentOf(parent) >= parentIndent) continue;
            parentIndent = indentOf(parent);
            if (/^\s*on\s+(?:click|double click|select)\s*$/.test(parent)) {
                return { kind: 'entries', entries: chain[0] === 'on' ? items.interactionChoiceItems : items.interactionActionItems };
            }
            if (/^\s*(?:screen|behavior|specification|reaction)\b/.test(parent)) break;
        }
    }
    if (chain[0] === 'on' && refusal !== undefined) return { kind: 'entries', entries: items.refusalItems };
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

    const headers = enclosingHeaders(lines, fences, lineIndex, effectiveIndent);
    if (chain[0] === 'for' && chain.includes('seed')) return { kind: 'events' };
    const entries = completionEntriesFor(chain, { headers, document: lines });
    const expansion = chain[0] === 'command' ? structureCompletion(lines, lineIndex, ' '.repeat(effectiveIndent), '', symbols) : null;
    return { kind: 'entries', entries: expansion ? [expansionEntry(expansion.text, effectiveIndent), ...entries] : entries };
}
