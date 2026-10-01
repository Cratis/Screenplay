// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ScreenDirectiveSyntax, ScreenSlotSyntax } from '@cratis/screenplay-compiler';
import { PrototypeCanvas } from './PrototypeCanvas';

// How large each placeholder is drawn, in the prototype window's pixels.
export const prototypeMetrics = {
    windowWidth: 900,
    minimumWindowHeight: 480,
    padding: 24,
    gap: 16,
    titleHeight: 32,
    buttonWidth: 160,
    buttonHeight: 40,
    tableHeight: 220,
    panelHeight: 120,
    fieldHeight: 28,
    codeHeight: 140,
} as const;

// Slots that span the whole width of their template - everything else sits side by side in a row.
const spanningSlots = new Set(['header', 'footer', 'title', 'toolbar', 'actions', 'body', 'content']);

interface Region {
    readonly x: number;
    readonly width: number;
}

// Lays a screen's directives out top to bottom within a region and returns the height they take. Actions
// that follow one another share a row; a template's slots become rows and columns; data only draws when
// nothing else in the screen presents it.
export function layoutDirectives(canvas: PrototypeCanvas, directives: readonly ScreenDirectiveSyntax[], region: Region, top: number, presentsData: boolean): number {
    const { gap } = prototypeMetrics;
    let y = top;
    let buttons: string[] = [];
    const flushButtons = () => {
        y = placeButtons(canvas, buttons, region, y);
        buttons = [];
    };
    for (const directive of directives) {
        if (directive.kind === 'ScreenActionSyntax' || directive.kind === 'ScreenNavigateSyntax') {
            buttons.push(directive.kind === 'ScreenActionSyntax' ? directive.label ?? directive.command : directive.screen);
            continue;
        }
        flushButtons();
        const height = layoutDirective(canvas, directive, region, y, presentsData);
        if (height > 0) {
            y += height + gap;
        }
    }
    flushButtons();
    return Math.max(0, y - top - gap);
}

function layoutDirective(canvas: PrototypeCanvas, directive: ScreenDirectiveSyntax, region: Region, y: number, presentsData: boolean): number {
    const metrics = prototypeMetrics;
    switch (directive.kind) {
        case 'ScreenTitleSyntax':
            canvas.place('label', directive.text, region.x, y, region.width, metrics.titleHeight);
            return metrics.titleHeight;
        case 'ScreenDataSyntax':
            if (presentsData) {
                return 0;
            }
            return place(canvas, directive.type.isCollection ? 'data-table' : 'panel', directive.type.name, region, y,
                directive.type.isCollection ? metrics.tableHeight : metrics.panelHeight);
        case 'ScreenTableSyntax':
            return place(canvas, 'data-table', directive.target, region, y, metrics.tableHeight);
        case 'ScreenSummarySyntax':
            return place(canvas, 'panel', directive.target, region, y, metrics.fieldHeight * (directive.fields.length + 1));
        case 'ScreenCodeSyntax':
            return place(canvas, 'content-area', directive.code.language, region, y, metrics.codeHeight);
        case 'ScreenSectionSyntax':
            return layoutDirectives(canvas, directive.directives, region, y, presentsData);
        case 'ScreenTemplateReferenceSyntax':
            return layoutSlots(canvas, directive.slots, region, y, presentsData);
        default:
            return 0;
    }
}

function place(canvas: PrototypeCanvas, type: string, name: string, region: Region, y: number, height: number): number {
    canvas.place(type, name, region.x, y, region.width, height);
    return height;
}

function placeButtons(canvas: PrototypeCanvas, names: readonly string[], region: Region, top: number): number {
    const { buttonWidth, buttonHeight, gap } = prototypeMetrics;
    const perRow = Math.max(1, Math.floor((region.width + gap) / (buttonWidth + gap)));
    names.forEach((name, index) => {
        const column = index % perRow;
        const row = Math.floor(index / perRow);
        canvas.place(index === 0 ? 'button-hot' : 'button', name, region.x + column * (buttonWidth + gap), top + row * (buttonHeight + gap), buttonWidth, buttonHeight);
    });
    const rows = Math.ceil(names.length / perRow);
    return rows === 0 ? top : top + rows * (buttonHeight + gap);
}

// A template's spanning slots are rows of their own; the slots between them share a row as columns.
function layoutSlots(canvas: PrototypeCanvas, slots: readonly ScreenSlotSyntax[], region: Region, top: number, presentsData: boolean): number {
    const { gap } = prototypeMetrics;
    let y = top;
    let columns: ScreenSlotSyntax[] = [];
    const flushColumns = () => {
        const height = columns.length === 0 ? 0 : layoutColumns(canvas, columns, region, y, presentsData);
        y += height > 0 ? height + gap : 0;
        columns = [];
    };
    for (const slot of slots) {
        if (spanningSlots.has(slot.name)) {
            flushColumns();
            const height = layoutDirectives(canvas, slot.directives, region, y, presentsData);
            y += height > 0 ? height + gap : 0;
        } else {
            columns.push(slot);
        }
    }
    flushColumns();
    return Math.max(0, y - top - gap);
}

function layoutColumns(canvas: PrototypeCanvas, slots: readonly ScreenSlotSyntax[], region: Region, top: number, presentsData: boolean): number {
    const { gap } = prototypeMetrics;
    const width = (region.width - gap * (slots.length - 1)) / slots.length;
    return Math.max(...slots.map((slot, index) =>
        layoutDirectives(canvas, slot.directives, { x: region.x + index * (width + gap), width }, top, presentsData)));
}
