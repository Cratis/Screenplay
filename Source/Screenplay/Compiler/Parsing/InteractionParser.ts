// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorParameterSyntax, BehaviorSyntax, InteractionAlternativeSyntax, InteractionBindingSyntax, InteractionOtherwiseSyntax, InteractionTriggerKind, InteractionTriggerSyntax, UsesBehaviorSyntax } from '../Syntax/Interactions';
import { parseCondition } from './ConditionParser';
import { parseDescription } from './DescriptionParser';
import { parseFile } from './ImplementationParser';
import { collectInputUseFacts } from './InputUses';
import { validateInteractionBodies } from './InteractionAlternatives';
import { parseInteractionAction, parseInteractionActionList } from './InteractionActions';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef } from './PropertyLineParser';
import { SourceLine, locationOf } from './SourceLine';

const builtIns: Readonly<Record<string, InteractionTriggerKind>> = { click: 'Click', 'double click': 'DoubleClick', select: 'Select', submit: 'Submit', change: 'Change', load: 'Load', unload: 'Unload', enter: 'Enter', leave: 'Leave' };
const units = { second: 'Seconds', seconds: 'Seconds', minute: 'Minutes', minutes: 'Minutes', hour: 'Hours', hours: 'Hours', day: 'Days', days: 'Days' } as const;

/** The real parser consumes the committed reader; a lookahead collects its existing authoring facts. */
export function prepareInteractionBody(context: ParserContext, header: SourceLine): { body: ParserContext; lines: readonly SourceLine[] } {
    const probe = new ParserContext(context.reader.fork(), context.path, context.languages);
    probe.sourceOptions = context.sourceOptions;
    probe.scope = context.scope;
    const lines = collectInputUseFacts(probe, header);
    context.inputUses.push(...probe.inputUses);
    context.continuationValues.push(...probe.continuationValues);
    // This remains the sole owner of the established interaction grammar diagnostics and ancestry facts.
    validateInteractionBodies(context, lines);
    const body = new ParserContext(context.reader, context.path, context.languages);
    body.sourceOptions = context.sourceOptions;
    return { body, lines };
}

export function interactionBody(context: ParserContext, header: SourceLine): ParserContext {
    return prepareInteractionBody(context, header).body;
}

export function interactionAttachments(behaviors: readonly BehaviorSyntax[], usedBehaviors: readonly UsesBehaviorSyntax[]): { behaviors?: readonly BehaviorSyntax[]; usedBehaviors?: readonly UsesBehaviorSyntax[] } {
    return { ...(behaviors.length > 0 ? { behaviors } : {}), ...(usedBehaviors.length > 0 ? { usedBehaviors } : {}) };
}

export function parseBehavior(context: ParserContext, header: SourceLine): BehaviorSyntax {
    return parseBehaviorBody(interactionBody(context, header), header);
}

function anonymousBehavior(header: SourceLine, bindings: readonly InteractionBindingSyntax[]): BehaviorSyntax {
    return { kind: 'BehaviorSyntax', name: null, parameters: [], bindings, description: null, order: null, file: null, location: locationOf(header) };
}

export function parseInlineBehavior(context: ParserContext, header: SourceLine): BehaviorSyntax {
    return readInlineBehavior(interactionBody(context, header), header);
}

export function readInlineBehavior(context: ParserContext, header: SourceLine): BehaviorSyntax {
    const binding = parseBinding(context, header);
    return anonymousBehavior(header, binding === null ? [] : [binding]);
}

export function parseUsesBehavior(context: ParserContext, header: SourceLine): UsesBehaviorSyntax {
    return readUsesBehavior(interactionBody(context, header), header);
}

export function readUsesBehavior(context: ParserContext, header: SourceLine): UsesBehaviorSyntax {
    const arguments_: UsesBehaviorSyntax['arguments'][number][] = [];
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const argument = /^([A-Za-z_]\w*)\s+(.+)$/.exec(child.content);
        if (argument !== null) arguments_.push({ kind: 'BehaviorArgumentSyntax', name: argument[1], value: argument[2].trim(), location: locationOf(child) });
    }
    return { kind: 'UsesBehaviorSyntax', behavior: /^uses\s+([A-Za-z_]\w*)$/.exec(header.content)?.[1] ?? '', arguments: arguments_, location: locationOf(header) };
}

function parseBehaviorBody(context: ParserContext, header: SourceLine): BehaviorSyntax {
    const parameters: BehaviorParameterSyntax[] = [];
    const bindings: InteractionBindingSyntax[] = [];
    const metadata: { description: string | null; order: number | null; file: BehaviorSyntax['file'] } = { description: null, order: null, file: null };
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(child.content)) {
            case 'parameter': readParameter(child, parameters); break;
            case 'description': metadata.description = parseDescription(context, child, metadata.description, `behavior '${header.content.substring('behavior'.length).trim()}'`); break;
            case 'order': metadata.order = readOrder(child) ?? metadata.order; break;
            case 'file': metadata.file = parseFile(context, child); break;
            case 'on': {
                const binding = parseBinding(context, child);
                if (binding !== null) bindings.push(binding);
                break;
            }
            default: context.skipBlock(child.indent); break;
        }
    }
    return { ...anonymousBehavior(header, bindings), ...metadata, name: /^behavior\s+([A-Za-z_]\w*)$/.exec(header.content)?.[1] ?? '', parameters };
}

function readParameter(line: SourceLine, parameters: BehaviorParameterSyntax[]): void {
    const match = /^parameter\s+([A-Za-z_]\w*)(?:\s+([\w.]+(?:\[\])?))?$/.exec(line.content);
    if (match === null || parameters.some(parameter => parameter.name === match[1])) return;
    parameters.push({ kind: 'BehaviorParameterSyntax', name: match[1], type: match[2] === undefined ? null : parseTypeRef(match[2], locationOf(line)), location: locationOf(line) });
}

function readOrder(line: SourceLine): number | null {
    const match = /^order\s+(-?\d+)$/.exec(line.content);
    const value = match === null ? NaN : Number(match[1]);
    return Number.isInteger(value) && value >= -2147483648 && value <= 2147483647 ? value : null;
}

function parseBinding(context: ParserContext, header: SourceLine): InteractionBindingSyntax | null {
    const trigger = parseInteractionTrigger(header);
    if (trigger === null) { context.skipBlock(header.indent); return null; }
    const actions: InteractionBindingSyntax['actions'][number][] = [];
    const alternatives: InteractionAlternativeSyntax[] = [];
    let condition: string | null = null;
    let otherwise: InteractionOtherwiseSyntax | null = null;
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'when') {
            const alternative = readAlternative(context, child);
            if (alternative !== null) alternatives.push(alternative);
        } else if (keyword === 'otherwise') otherwise = { kind: 'InteractionOtherwiseSyntax', actions: parseInteractionActionList(context, child), location: locationOf(child) };
        else if (/^where\s+(.+)$/.test(child.content)) condition ??= child.content.replace(/^where\s+/, '').trim();
        else {
            const action = parseInteractionAction(context, child);
            if (action !== null) actions.push(action);
        }
    }
    return { kind: 'InteractionBindingSyntax', trigger, condition, actions, alternatives, otherwise, location: locationOf(header) };
}

function readAlternative(context: ParserContext, header: SourceLine): InteractionAlternativeSyntax | null {
    const text = header.content.substring('when'.length).trim();
    const condition = parseCondition(context, text, locationOf(header), true);
    const actions = parseInteractionActionList(context, header);
    return condition === null ? null : { kind: 'InteractionAlternativeSyntax', condition, actions, location: locationOf(header) };
}

function intervalAmount(text: string): number {
    const amount = Number(text);
    return amount <= 2147483647 ? amount : 1;
}

function parseInteractionTrigger(line: SourceLine): InteractionTriggerSyntax | null {
    const text = /^on\s+(.+)$/.exec(line.content)?.[1].trim();
    const location = locationOf(line);
    if (text === undefined) return null;
    if (Object.hasOwn(builtIns, text)) return { kind: 'BuiltInInteractionTriggerSyntax', syntaxKind: builtIns[text], location };
    const event = /^event\s+([A-Za-z_]\w*(?:\.\w+)*)$/.exec(text);
    if (event !== null) return { kind: 'EventInteractionTriggerSyntax', eventName: event[1], location };
    const interval = /^interval\s+(\d+)\s+(seconds?|minutes?|hours?|days?)$/.exec(text);
    if (interval !== null) return { kind: 'IntervalInteractionTriggerSyntax', amount: intervalAmount(interval[1]), unit: units[interval[2] as keyof typeof units], location };
    return /^(?:[A-Z]\w*(?:\.[A-Z]\w*)?|[a-z]\w*)$/.test(text) ? { kind: 'ApplicationTriggerInteractionTriggerSyntax', triggerName: text, location } : null;
}
