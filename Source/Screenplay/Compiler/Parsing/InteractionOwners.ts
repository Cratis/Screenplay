// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogTemplateSyntax, InteractionOwner, LayoutSyntax, ScreenTemplateSyntax, SlotSyntax } from '../Syntax/InteractionOwners';
import { BehaviorSyntax, UsesBehaviorSyntax } from '../Syntax/Interactions';
import { interactionBody, readInlineBehavior, readUsesBehavior } from './InteractionParser';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { SourceLine, locationOf } from './SourceLine';
import { stringBody, stringLiteralText } from './StringLiteral';

interface OwnerBody extends Omit<InteractionOwner, 'kind'> {
    readonly fitsSlot: string | null;
    readonly displayName: string | null;
    readonly description: string | null;
}

const slotPattern = /^([A-Za-z_]\w*)(?:\s+(page|pane|panel))?(?:\s+(routed)(?:\s+by\s+([A-Za-z_]\w*))?)?(?:\s+(primary))?(?:\s+(optional))?$/;
const literal = new RegExp(`^"(${stringBody})"$`);
const roles = { page: 'Page', pane: 'Pane', panel: 'Panel' } as const;

export function parseLayout(context: ParserContext, header: SourceLine): LayoutSyntax {
    const { fitsSlot: _fits, displayName: _display, description: _description, ...body } = readOwner(interactionBody(context, header), header);
    return { kind: 'LayoutSyntax', ...body };
}

export function parseScreenTemplate(context: ParserContext, header: SourceLine): ScreenTemplateSyntax {
    return { kind: 'ScreenTemplateSyntax', ...readOwner(interactionBody(context, header), header) };
}

export function parseDialogTemplate(context: ParserContext, header: SourceLine): DialogTemplateSyntax {
    const { fitsSlot: _fits, ...body } = readOwner(interactionBody(context, header), header);
    return { kind: 'DialogTemplateSyntax', ...body };
}

function readOwner(context: ParserContext, header: SourceLine): OwnerBody {
    const behaviors: BehaviorSyntax[] = [];
    const usedBehaviors: UsesBehaviorSyntax[] = [];
    const slots: SlotSyntax[] = [];
    const metadata = { category: null as string | null, templateType: null as string | null, fitsSlot: null as string | null, displayName: null as string | null, description: null as string | null, restrictsScopes: false, scopes: [] as string[], sliceKinds: [] as string[] };
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(child.content)) {
            case 'on': behaviors.push(readInlineBehavior(context, child)); break;
            case 'uses': usedBehaviors.push(readUsesBehavior(context, child)); break;
            case 'category': metadata.category = operand(child, 'category'); break;
            case 'type': metadata.templateType = operand(child, 'type'); break;
            case 'fits': metadata.fitsSlot = /^fits\s+slot\s+([A-Za-z_]\w*)$/.exec(child.content)?.[1] ?? null; break;
            case 'scopes': readScopes(child, metadata); break;
            default: readOwnerMember(context, child, slots, metadata); break;
        }
    }
    return { name: header.content.replace(/^(?:layout|screen\s+template|dialog\s+template)\s+/, ''), ...(slots.length > 0 ? { slots } : {}), behaviors, usedBehaviors, ...metadata, location: locationOf(header) };
}

function readOwnerMember(context: ParserContext, line: SourceLine, slots: SlotSyntax[], metadata: { displayName: string | null; description: string | null }): void {
    const slot = slotPattern.exec(line.content);
    if (slot !== null) {
        if (!slots.some(existing => existing.name === slot[1])) slots.push({ kind: 'SlotSyntax', name: slot[1], role: slot[2] === undefined ? null : roles[slot[2] as keyof typeof roles], routed: slot[3] !== undefined, routedBy: slot[4] ?? null, primary: slot[5] !== undefined, optional: slot[6] !== undefined, location: locationOf(line) });
    } else if (firstWord(line.content) === 'display') metadata.displayName = operand(line, 'display');
    else if (firstWord(line.content) === 'description') metadata.description = parseDescription(context, line, metadata.description, 'template');
    context.skipBlock(line.indent);
}

function operand(line: SourceLine, keyword: string): string {
    const text = line.content.substring(keyword.length).trim();
    const quoted = literal.exec(text);
    return quoted === null ? text : stringLiteralText(quoted[1]);
}

function readScopes(line: SourceLine, metadata: { restrictsScopes: boolean; scopes: string[]; sliceKinds: string[] }): void {
    const text = operand(line, 'scopes');
    metadata.restrictsScopes = true;
    metadata.scopes = text === 'none' ? [] : text.split(/\s*,\s*/).map(scope => scope.split(/\s+/)[0]);
    metadata.sliceKinds = [...text.matchAll(/\b(statechange|stateview)\b/gi)].map(match => match[1].toLowerCase() === 'statechange' ? 'StateChange' : 'StateView');
}
