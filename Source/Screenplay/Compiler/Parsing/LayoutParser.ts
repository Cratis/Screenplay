// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { SourceLocation } from '../Diagnostics/SourceLocation';
import type { ArrangementSyntax, DialogTemplateSyntax, LayoutSyntax, ScreenTemplateSyntax, SlotSyntax, TemplateExposedValueSyntax, TemplateSlotContentSyntax, TemplateSlotSyntax } from '../Syntax/Layouts';
import type { OutletSyntax } from '../Syntax/Outlets';
import type { SyntaxNode } from '../Syntax/SyntaxNode';
import { parseTemplateMetadata } from './TemplateMetadataParser';
import { pattern } from '../Text/patterns';
import { addSlot, childrenOf, parseArrangement } from './ArrangementParser';
import { firstWord } from './LineText';
import { isOutletDeclaration, parseOutletDeclaration } from './OutletDeclarationParser';
import { ParserContext } from './ParserContext';
import { parseTypeRef } from './PropertyLineParser';
import { parseDirective } from './ScreenParser';
import { renamedForSlotsAndOutlets, replacedForSlotsAndOutlets } from './SlotsAndOutletsMigration';
import { locationOf, SourceLine } from './SourceLine';

const slotPattern = pattern('^([a-z_]\\w*)(?:\\s+(page|pane|panel))?(?:\\s+(routed)(?:\\s+by\\s+([a-z_]\\w*))?)?(?:\\s+(primary))?(?:\\s+(optional))?(?:\\s+contributes\\s+([A-Za-z_]\\w*))?$');
const exposesPattern = pattern('^exposes\\s+([A-Za-z_]\\w*)(?:\\s+([\\w.]+(?:\\[\\])?))?$');
type LayoutNode = LayoutSyntax | ScreenTemplateSyntax | DialogTemplateSyntax;

class Body {
    readonly slots: SlotSyntax[] = [];
    readonly outlets: OutletSyntax[] = [];
    readonly exposes: TemplateExposedValueSyntax[] = [];
    readonly recursiveSlots: TemplateSlotSyntax[] = [];
    readonly behaviors: SyntaxNode[] = [];
    readonly usedBehaviors: SyntaxNode[] = [];
    readonly content: TemplateSlotContentSyntax[] = [];
    readonly directiveLocations: Record<string, SourceLocation> = {};
    arrangement: ArrangementSyntax | null = null;
    category: string | null = null;
    templateType: string | null = null;
    fitsSlot: string | null = null;
    fitsSlotLocation: SourceLocation | null = null;
    scopes: string[] | null = null;
    sliceKinds: string[] = [];
    displayName: string | null = null;
    description: string | null = null;
}

export function parseLayout(context: ParserContext, line: SourceLine, keyword: 'layout' | 'screen template' | 'dialog template'): LayoutNode {
    const match = pattern(`^${keyword.replace(' ', '\\s+')}\\s+([A-Za-z_]\\w*)$`).exec(line.content);
    const codes = { layout: DiagnosticCodes.InvalidLayoutDeclaration, 'screen template': DiagnosticCodes.InvalidScreenTemplateDeclaration, 'dialog template': DiagnosticCodes.InvalidDialogTemplateDeclaration };
    if (match === null) context.error(codes[keyword], `Invalid ${keyword} declaration '${line.content}' - expected '${keyword} <Name>'`, locationOf(line));
    const name = match?.[1] ?? (keyword === 'layout' ? firstWord(line.content.slice(keyword.length).trim()) : '');
    const body = new Body();
    for (const child of childrenOf(context, line)) readMember(context, child, { owner: keyword, name }, body);
    validateContent(context, keyword, name, body);
    const shared = { name, slots: body.slots, arrangement: body.arrangement, category: body.category, templateType: body.templateType,
        exposes: body.exposes, recursiveSlots: body.recursiveSlots, restrictsScopes: body.scopes !== null, scopes: body.scopes ?? [], sliceKinds: body.sliceKinds,
        ...(body.outlets.length === 0 ? {} : { outlets: body.outlets }),
        ...(body.behaviors.length === 0 ? {} : { behaviors: body.behaviors }),
        ...(body.usedBehaviors.length === 0 ? {} : { usedBehaviors: body.usedBehaviors }), location: locationOf(line) };
    // #1388: interactions retain authoring facts, but are not serialized as a full behavior AST.
    if (keyword === 'layout') return { kind: 'LayoutSyntax', ...shared };
    const metadata = { displayName: body.displayName, description: body.description, content: body.content };
    return keyword === 'screen template'
        ? { kind: 'ScreenTemplateSyntax', ...shared, ...metadata, fitsSlot: body.fitsSlot, fitsSlotLocation: body.fitsSlotLocation }
        : { kind: 'DialogTemplateSyntax', ...shared, ...metadata };
}

type Declaration = { readonly owner: string; readonly name: string };

function readMember(context: ParserContext, line: SourceLine, { owner, name }: Declaration, body: Body): void {
    const word = firstWord(line.content);
    if (['scopes', 'display', 'description', 'content'].includes(word) && !slotPattern.test(line.content)) {
        parseTemplateMetadata(context, line, owner, body);
        return;
    }
    switch (word) {
        case 'arrangement': readArrangement(context, line, owner, body); break;
        case 'fits': readFits(context, line, owner, body); break;
        case 'category': body.category = line.content.slice(word.length).trim(); body.directiveLocations.category = locationOf(line); break;
        case 'type': body.templateType = line.content.slice(word.length).trim(); break;
        case 'exposes': readExposed(context, line, body); break;
        case 'slot': body.recursiveSlots.push({ kind: 'TemplateSlotSyntax', name: line.content.slice(word.length).trim(), location: locationOf(line) }); break;
        case 'outlet': readOutlet(context, line, { owner, name }, body); break;
        case 'on': case 'uses': readBehavior(context, line, word === 'on' ? body.behaviors : body.usedBehaviors); break;
        default: readSlot(context, line, body); break;
    }
}

function readArrangement(context: ParserContext, line: SourceLine, owner: string, body: Body): void {
    if (body.arrangement !== null) {
        context.error(DiagnosticCodes.DuplicateArrangement, `This ${owner} already declares 'arrangement' - a ${owner} can have at most one`, locationOf(line));
        context.skipBlock(line.indent);
    } else body.arrangement = parseArrangement(context, line, owner, body.slots);
}

// A template holds behaviors under the names the C# syntax gives them; the body is the same placeholder a screen reads.
const behaviorKinds: Record<string, string> = { ScreenBehaviorSyntax: 'BehaviorSyntax', ScreenUsesBehaviorSyntax: 'UsesBehaviorSyntax' };

function readBehavior(context: ParserContext, line: SourceLine, into: SyntaxNode[]): void {
    const directive = parseDirective(context, line);
    if (directive !== undefined) into.push({ ...directive, kind: behaviorKinds[directive.kind] ?? directive.kind } as SyntaxNode);
}

function readFits(context: ParserContext, line: SourceLine, owner: string, body: Body): void {
    if (owner !== 'screen template') {
        context.error(DiagnosticCodes.FitsSlotNotAllowed, `A ${owner} cannot declare 'fits slot' - it occupies no slot of the structure it opens over`, locationOf(line));
        return;
    }
    const match = pattern('^fits\\s+slot\\s+([a-z_]\\w*)$').exec(line.content);
    if (match === null) context.error(DiagnosticCodes.InvalidFitsSlotDeclaration, `Invalid 'fits slot' declaration '${line.content}' - expected 'fits slot <name>'`, locationOf(line));
    else if (body.fitsSlot !== null) context.error(DiagnosticCodes.DuplicateFitsSlot, `This ${owner} already declares 'fits slot' - a ${owner} fills at most one slot of its parent`, locationOf(line));
    else { body.fitsSlot = match[1]; body.fitsSlotLocation = locationOf(line); }
}

function readExposed(context: ParserContext, line: SourceLine, body: Body): void {
    const match = exposesPattern.exec(line.content);
    if (match === null) context.error(DiagnosticCodes.UnknownLayoutDirective, `Invalid exposed template value '${line.content}' - expected 'exposes <name> [<Type>]'`, locationOf(line));
    else body.exposes.push({ kind: 'TemplateExposedValueSyntax', name: match[1], type: match[2] === undefined ? null : parseTypeRef(match[2], locationOf(line)), location: locationOf(line) });
}

function readOutlet(context: ParserContext, line: SourceLine, { owner, name }: Declaration, body: Body): void {
    if (!isOutletDeclaration(line.content)) {
        const renamed = line.content.slice('outlet'.length).trim();
        context.error(DiagnosticCodes.RemovedTemplateOutlet, renamedForSlotsAndOutlets(`'outlet ${renamed}' in a ${owner}`, `slot ${renamed}`), locationOf(line));
        body.recursiveSlots.push({ kind: 'TemplateSlotSyntax', name: renamed, location: locationOf(line) });
        return;
    }
    const outlet = parseOutletDeclaration(context, line, owner);
    if (outlet === undefined) return;
    if (body.outlets.some(existing => existing.name === outlet.name)) context.error(DiagnosticCodes.DuplicateOutlet, `The ${owner} '${name}' already declares outlet '${outlet.name}' - an outlet is declared once`, locationOf(line));
    else body.outlets.push(outlet);
}

function readSlot(context: ParserContext, line: SourceLine, body: Body): void {
    const match = slotPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidLayoutSlotName, `Invalid slot declaration '${line.content}' - expected an identifier, optionally followed by a role ('page', 'pane' or 'panel'), 'routed [by <param>]', 'primary' and 'optional', in that order`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    if (context.peekChild(line.indent) !== undefined) {
        context.error(DiagnosticCodes.UnknownLayoutDirective, `Unexpected block under '${line.content}' - a slot declaration names a slot and nothing more; arrange slots in the 'arrangement' block`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    if (match[7] !== undefined) context.error(DiagnosticCodes.RemovedSlotContribution, replacedForSlotsAndOutlets(`'contributes ${match[7]}' on slot '${match[1]}'`, `outlet ${match[7]} of navigation in ${match[1]}`), locationOf(line));
    const roles = { page: 'Page', pane: 'Pane', panel: 'Panel' } as const;
    addSlot(body.slots, { kind: 'SlotSyntax', name: match[1], role: roles[match[2] as keyof typeof roles] ?? null, routed: match[3] !== undefined, routedBy: match[4] ?? null, primary: match[5] !== undefined, optional: match[6] !== undefined, location: locationOf(line) });
}

function validateContent(context: ParserContext, owner: string, name: string, body: Body): void {
    for (const content of body.content) {
        if (!body.slots.some(slot => slot.name === content.slot)) context.error(DiagnosticCodes.UnknownTemplateContentSlot, `The ${owner} '${name}' provides content for slot '${content.slot}', which it does not declare`, content.location);
    }
}
