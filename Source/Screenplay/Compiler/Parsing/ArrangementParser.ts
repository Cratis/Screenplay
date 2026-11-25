// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { ArrangementContainerSyntax, ArrangementNodeSyntax, ArrangementOverrideSyntax, ArrangementSyntax, PlaceSyntax, SlotSyntax, VariantSyntax } from '../Syntax/Layouts';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const containerPattern = /^(row|column|grid)(?:\s+gap\s+(\d+))?(?:\s+columns\s+(\d+))?(?:\s+rows\s+(\d+))?(?:\s+(grow)(?:\s+(\d+(?:\.\d+)?))?)?(?:\s+span\s+(\d+))?$/;
const slotPattern = /^([a-z_]\w*)(?:\s+width\s+(\d+))?(?:\s+height\s+(\d+))?(?:\s+(grow)(?:\s+(\d+(?:\.\d+)?))?)?(?:\s+span\s+(\d+))?$/;
const overridePattern = /^when\s+(?:width\s+(compact|regular)(?:\s*,\s*height\s+(compact|regular))?|height\s+(compact|regular))$/;
const variantPattern = /^variant\s+width\s+(compact|regular)\s*,\s*height\s+(compact|regular)$/;
const placePattern = /^place\s+([a-z_]\w*)\s+(?:(hidden)|at\s+(\d+)\s*,\s*(\d+)\s+size\s+(fill|\d+)\s*,\s*(fill|\d+))$/;
const numberOrNull = (value: string | undefined): number | null => value === undefined ? null : Number(value);

export function addSlot(slots: SlotSyntax[], slot: SlotSyntax): void {
    if (!slots.some(existing => existing.name === slot.name)) slots.push(slot);
}

function flat(line: SourceLine, children: ArrangementNodeSyntax[]): ArrangementContainerSyntax {
    return { kind: 'ArrangementContainerSyntax', syntaxKind: 'Flat', children, location: locationOf(line) };
}

function collectSlots(node: ArrangementNodeSyntax, slots: SlotSyntax[]): void {
    if (node.kind === 'ArrangementSlotSyntax') addSlot(slots, { kind: 'SlotSyntax', name: node.name, location: node.location });
    else node.children.forEach(child => collectSlots(child, slots));
}

export function parseArrangement(context: ParserContext, line: SourceLine, owner: string, slots: SlotSyntax[]): ArrangementSyntax {
    const match = /^arrangement\s+(flow|freeform)$/.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidArrangementDeclaration, `Invalid arrangement declaration '${line.content}' - expected 'arrangement flow' or 'arrangement freeform'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ArrangementSyntax', mode: 'Flow', root: flat(line, []), overrides: [], location: locationOf(line) };
    }
    return match[1] === 'flow' ? parseFlow(context, line, owner, slots) : parseFreeform(context, line, owner, slots);
}

function parseFlow(context: ParserContext, line: SourceLine, owner: string, slots: SlotSyntax[]): ArrangementSyntax {
    const children: ArrangementNodeSyntax[] = [];
    const overrides: ArrangementOverrideSyntax[] = [];
    for (const child of childrenOf(context, line)) {
        if (firstWord(child.content) === 'when') {
            const override = parseOverride(context, child);
            if (overrides.some(existing => existing.width === override.width && existing.height === override.height)) {
                context.error(DiagnosticCodes.DuplicateArrangementOverride, "This arrangement already declares a 'when' override for this width/height combination", locationOf(child));
            } else overrides.push(override);
        } else if (firstWord(child.content) === 'variant') {
            context.error(DiagnosticCodes.ArrangementDirectiveMismatch, `The ${owner} arranges by 'flow' but declares a 'variant' - variants belong to 'arrangement freeform'`, locationOf(child));
            context.skipBlock(child.indent);
        } else {
            const node = parseNode(context, child);
            if (node !== undefined) children.push(node);
        }
    }
    const root = flat(line, children);
    collectSlots(root, slots);
    return { kind: 'ArrangementSyntax', mode: 'Flow', root, overrides, location: locationOf(line) };
}

// Yield only immediate children; the caller consumes each child's body before requesting the next.
export function* childrenOf(context: ParserContext, line: SourceLine): Generator<SourceLine> {
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        yield child;
    }
}

function parseFreeform(context: ParserContext, line: SourceLine, owner: string, slots: SlotSyntax[]): ArrangementSyntax {
    const variants: VariantSyntax[] = [];
    for (const child of childrenOf(context, line)) {
        if (firstWord(child.content) !== 'variant') {
            context.error(DiagnosticCodes.ArrangementDirectiveMismatch, `Unexpected '${child.content}' - the ${owner} arranges by 'freeform', whose body is made of 'variant' blocks`, locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        const variant = parseVariant(context, child, slots);
        if (variants.some(existing => existing.width === variant.width && existing.height === variant.height)) {
            context.error(DiagnosticCodes.DuplicateVariant, `This arrangement already declares a 'variant' for width ${variant.width}, height ${variant.height}`, locationOf(child));
        } else variants.push(variant);
    }
    return { kind: 'ArrangementSyntax', mode: 'Freeform', variants, location: locationOf(line) };
}

function parseOverride(context: ParserContext, line: SourceLine): ArrangementOverrideSyntax {
    const match = overridePattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidArrangementOverride, `Invalid 'when' declaration '${line.content}' - expected 'when width <compact|regular>[, height <compact|regular>]' or 'when height <compact|regular>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ArrangementOverrideSyntax', width: null, height: null, root: flat(line, []), location: locationOf(line) };
    }
    return { kind: 'ArrangementOverrideSyntax', width: match[1] ?? null, height: match[2] ?? match[3] ?? null, root: flat(line, parseNodes(context, line)), location: locationOf(line) };
}

function parseNodes(context: ParserContext, line: SourceLine): ArrangementNodeSyntax[] {
    return [...childrenOfNodes(context, line)];
}

function* childrenOfNodes(context: ParserContext, line: SourceLine): Generator<ArrangementNodeSyntax> {
    for (const child of childrenOf(context, line)) {
        const node = parseNode(context, child);
        if (node !== undefined) yield node;
    }
}

function parseNode(context: ParserContext, line: SourceLine): ArrangementNodeSyntax | undefined {
    return ['row', 'column', 'grid'].includes(firstWord(line.content)) ? parseContainer(context, line) : parseSlot(context, line);
}

function parseContainer(context: ParserContext, line: SourceLine): ArrangementContainerSyntax | undefined {
    const match = containerPattern.exec(line.content);
    if (match === null) {
        const word = firstWord(line.content);
        context.error(DiagnosticCodes.InvalidArrangementContainer, `Invalid ${word} '${line.content}' - expected '${word} [gap <n>] [columns <n>] [rows <n>] [grow [<number>]] [span <n>]'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    const kinds = { row: 'Row', column: 'Column', grid: 'Grid' } as const;
    return { kind: 'ArrangementContainerSyntax', syntaxKind: kinds[match[1] as keyof typeof kinds], children: parseNodes(context, line), gap: numberOrNull(match[2]), columns: numberOrNull(match[3]), rows: numberOrNull(match[4]), grow: match[5] === undefined ? null : Number(match[6] ?? 1), span: numberOrNull(match[7]), location: locationOf(line) };
}

function parseSlot(context: ParserContext, line: SourceLine): ArrangementNodeSyntax | undefined {
    const match = slotPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidArrangementSlotAttributes, `Invalid slot '${line.content}' - expected an identifier, optionally followed by 'width <n>', 'height <n>', 'grow [<number>]' or 'span <n>'`, locationOf(line));
        return undefined;
    }
    return { kind: 'ArrangementSlotSyntax', name: match[1], width: numberOrNull(match[2]), height: numberOrNull(match[3]), grow: match[4] !== undefined, growFactor: numberOrNull(match[5]), span: numberOrNull(match[6]), location: locationOf(line) };
}

function parseVariant(context: ParserContext, line: SourceLine, slots: SlotSyntax[]): VariantSyntax {
    const match = variantPattern.exec(line.content);
    const places: PlaceSyntax[] = [];
    if (match === null) {
        context.error(DiagnosticCodes.InvalidVariantDeclaration, `Invalid variant declaration '${line.content}' - expected 'variant width <compact|regular>, height <compact|regular>'`, locationOf(line));
        context.skipBlock(line.indent);
    } else {
        readPlaces(context, line, places, slots);
    }
    return { kind: 'VariantSyntax', width: match?.[1] ?? '', height: match?.[2] ?? '', places, location: locationOf(line) };
}

function readPlaces(context: ParserContext, line: SourceLine, places: PlaceSyntax[], slots: SlotSyntax[]): void {
    for (const child of childrenOf(context, line)) {
        const place = parsePlace(context, child);
        if (place === undefined) continue;
        if (places.some(existing => existing.slotName === place.slotName)) {
            context.error(DiagnosticCodes.DuplicatePlaceInVariant, `Slot '${place.slotName}' is already placed in this variant`, locationOf(child));
            continue;
        }
        places.push(place);
        addSlot(slots, { kind: 'SlotSyntax', name: place.slotName, location: place.location });
    }
}

function parsePlace(context: ParserContext, line: SourceLine): PlaceSyntax | undefined {
    const match = placePattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidPlaceDeclaration, `Invalid place declaration '${line.content}' - expected 'place <Slot> hidden' or 'place <Slot> at <x>,<y> size <w>,<h>'`, locationOf(line));
        return undefined;
    }
    return { kind: 'PlaceSyntax', slotName: match[1], hidden: match[2] !== undefined, x: numberOrNull(match[3]), y: numberOrNull(match[4]), sizeWidth: match[5] ?? null, sizeHeight: match[6] ?? null, location: locationOf(line) };
}
