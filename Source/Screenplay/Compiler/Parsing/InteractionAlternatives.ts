// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ShadowingAlternative } from './GuardedActionShadowing';
import { dotNetWhitespace, pattern } from '../Text/patterns';
import { parseCondition } from './ConditionParser';
import { validateCondition } from './GuardedActionParser';
import { firstWord } from './LineText';
import { LineReader } from './LineReader';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

// Authoring facts only: interaction bodies are still opaque in the serialized TS AST.
export interface InteractionAncestor {
    readonly keyword: string;
    readonly location: SourceLocation;
}

export interface GuardedInteractionBinding {
    readonly location: SourceLocation;
    readonly ancestors: readonly InteractionAncestor[];
    readonly alternatives: readonly ShadowingAlternative[];
}

const screenTemplate = pattern(`^screen${dotNetWhitespace}+template(?:${dotNetWhitespace}|$)`);

// Capture attachment ancestry before parsing consumes opaque bodies. Fenced code is not Screenplay.
export function interactionAncestry(lines: readonly SourceLine[]): ReadonlyMap<number, readonly InteractionAncestor[]> {
    const result = new Map<number, readonly InteractionAncestor[]>();
    const ancestors: SourceLine[] = [];
    let fenced = false;
    for (const line of lines) {
        if (line.content.startsWith('```')) {
            fenced = !fenced;
            continue;
        }
        if (fenced || line.content.length === 0 || line.content.startsWith('//')) continue;
        while (ancestors.length > 0 && ancestors[ancestors.length - 1].indent >= line.indent) ancestors.pop();
        if (firstWord(line.content) === 'on') result.set(line.number, ancestors.map(ancestor => ({ keyword: screenTemplate.test(ancestor.content) ? 'screen template' : firstWord(ancestor.content), location: locationOf(ancestor) })));
        ancestors.push(line);
    }
    return result;
}

const inline = pattern(`^when${dotNetWhitespace}+([^\\n]+)${dotNetWhitespace}+execute${dotNetWhitespace}+([A-Za-z_]\\w*(?:\\.\\w+)*)$`);
const trigger = pattern(`^on${dotNetWhitespace}+([^\\n]+)$`);
const where = pattern(`^where${dotNetWhitespace}+([^\\n]+)$`);
const actions = new Set(['execute', 'navigate', 'open', 'close', 'refresh', 'set', 'notify', 'confirm', 'raise']);

// Interaction bodies remain opaque in the TS AST. Check their structure instead of silently accepting
// alternatives the C# compiler refuses. Input collection supplies only significant, non-fenced lines.
export function validateInteractionBodies(context: ParserContext, lines: readonly SourceLine[]): void {
    for (let index = 0; index < lines.length; index++) {
        const header = lines[index];
        const on = trigger.exec(header.content);
        if (on === null || on[1].startsWith('row-click ')) continue;
        const body: SourceLine[] = [];
        for (let child = index + 1; child < lines.length && lines[child].indent > header.indent; child++) body.push(lines[child]);
        const children = body.filter(line => line.indent === body[0]?.indent);
        const branches = children.filter(line => ['when', 'otherwise'].includes(firstWord(line.content)));
        const supported = ['click', 'double click', 'select'].includes(on[1].trim());
        const guards = children.filter(line => where.test(line.content));
        const alternatives: ShadowingAlternative[] = [];
        let sawOtherwise = false;
        for (const branch of branches) {
            const isWhen = firstWord(branch.content) === 'when';
            const match = isWhen ? inline.exec(branch.content) : null;
            if (isWhen) {
                if (sawOtherwise) context.error(DiagnosticCodes.MisplacedActionOtherwise, "A 'when' alternative must precede 'otherwise'", locationOf(branch));
                const condition = parseCondition(context, match?.[1] ?? branch.content.slice(4).trim(), locationOf(branch), true);
                if (condition !== null) {
                    validateCondition(context, condition);
                    alternatives.push({ condition, location: locationOf(branch), message: "This 'when' alternative is shadowed by earlier alternatives in this interaction" });
                }
                if (match !== null) context.error(DiagnosticCodes.InlineInteractionAlternative, "Interaction alternatives require an indented action list, not 'when <condition> execute <Command>'", locationOf(branch));
            } else {
                if (sawOtherwise || branch.content !== 'otherwise') context.error(DiagnosticCodes.MisplacedActionOtherwise, "An interaction permits one final block-form 'otherwise'", locationOf(branch));
                sawOtherwise = true;
            }
            const position = body.indexOf(branch);
            const nested = body[position + 1];
            if (match === null && (nested === undefined || nested.indent <= branch.indent || !actions.has(firstWord(nested.content)))) {
                context.error(DiagnosticCodes.InteractionAlternativeWithoutActions, 'An interaction alternative requires a non-empty action list', locationOf(branch));
            }
        }
        if (branches.length > 0) {
            if (!supported) context.error(DiagnosticCodes.UnsupportedInteractionAlternatives, 'Interaction alternatives are only supported on click, double click and select', locationOf(header));
            if (guards.length > 0 || children.some(line => actions.has(firstWord(line.content)))) {
                context.error(DiagnosticCodes.MixedInteractionAlternatives, "Interaction alternatives cannot mix with plain actions or 'where'", locationOf(header));
            }
            if (!branches.some(line => firstWord(line.content) === 'when')) context.error(DiagnosticCodes.GuardedActionWithoutAlternatives, "A guarded interaction requires at least one 'when' alternative", locationOf(header));
        }
        if (supported) for (const guard of guards) deprecateWhere(context, guard);
        if (alternatives.length > 0) context.guardedInteractions.push({ location: locationOf(header), ancestors: context.interactionAncestors.get(header.number) ?? [], alternatives });
    }
}

function deprecateWhere(context: ParserContext, line: SourceLine): void {
    const probe = new ParserContext(new LineReader([]));
    probe.sourceOptions = context.sourceOptions;
    const condition = parseCondition(probe, where.exec(line.content)![1].trim(), locationOf(line), true);
    if (condition !== null) validateCondition(probe, condition);
    const message = "Opaque 'where' on click, double click and select is deprecated; use block-form 'when' item conditions";
    if (condition !== null && probe.diagnostics.length === 0) context.warning(DiagnosticCodes.LegacyInteractionWhere, message, locationOf(line));
    else context.information(DiagnosticCodes.LegacyInteractionWhere, message, locationOf(line));
}
