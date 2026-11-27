// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { ScreenComponentSyntax, ScreenDirectiveSyntax, ScreenShowSyntax, ScreenSyntax } from '../Syntax/Screens';
import { OutletInventory, screenDirectives } from './OutletInventory';
import { ParserContext } from './ParserContext';
import { hasComponentInteractions } from './ScreenParser';

const dataKinds = ['ScreenDataSyntax', 'ScreenTableSyntax', 'ScreenSummarySyntax'];

export function validateScreenBindings(inventory: OutletInventory, context: ParserContext): void {
    const screens = inventory.slices.flatMap(entry => entry.slice.screens);
    for (const { slice } of inventory.slices) {
        for (const screen of slice.screens) {
            const elements = [...screenDirectives(screen.directives)];
            const shows = elements.filter(element => element.kind === 'ScreenShowSyntax');
            shows.forEach(show => checkShow(show, { screen, elements, screens }, context));
            if (showsData(slice.type, elements)) warnAboutUnboundComponents(screen, elements, new Set(shows.map(show => show.element)), context);
        }
    }
}

function showsData(sliceType: string, elements: readonly ScreenDirectiveSyntax[]): boolean {
    return sliceType === 'StateView' || elements.some(element => dataKinds.includes(element.kind));
}

function warnAboutUnboundComponents(screen: ScreenSyntax, elements: readonly ScreenDirectiveSyntax[], selections: ReadonlySet<string>, context: ParserContext): void {
    for (const element of elements) {
        if (element.kind === 'ScreenComponentSyntax' && isUnbound(element) && !selections.has(element.name)) context.warning(DiagnosticCodes.UnboundScreenText, `Component '${element.name}' on screen '${screen.name}' shows data but is bound to nothing and given no value, so it can only show sample text - bind it with 'context' or 'property <path> from <binding>', or give it static text with 'property <path> = "..."'`, element.location);
    }
}

function isUnbound(component: ScreenComponentSyntax): boolean {
    return component.context === null && component.properties.length === 0 && component.exposes.length === 0 && component.slots.length === 0 && !hasComponentInteractions(component);
}

type ShowScope = { readonly screen: ScreenSyntax; readonly elements: readonly ScreenDirectiveSyntax[]; readonly screens: readonly ScreenSyntax[] };

const standsInSlot = (screen: ScreenSyntax, show: ScreenShowSyntax): boolean =>
    screen.directives.some(directive => directive.kind === 'ScreenTemplateReferenceSyntax' && directive.slots.some(slot => slot.directives.includes(show)));

function checkShow(show: ScreenShowSyntax, { screen, elements, screens }: ShowScope, context: ParserContext): void {
    if (!standsInSlot(screen, show)) {
        context.error(DiagnosticCodes.InvalidScreenShow, `'show ${show.screen}' on screen '${screen.name}' stands outside a template slot - a slot shows another screen`, show.location);
        return;
    }
    const target = screens.find(candidate => candidate.name === show.screen.split('.').at(-1));
    if (target === undefined) {
        context.error(DiagnosticCodes.InvalidScreenShow, `Screen '${screen.name}' shows screen '${show.screen}', which the document does not declare`, show.location);
        return;
    }
    if (!elements.some(element => element.kind === 'ScreenTableSyntax' ? element.target === show.element : element.kind === 'ScreenComponentSyntax' && element.name === show.element)) {
        context.error(DiagnosticCodes.InvalidScreenShow, `Screen '${screen.name}' shows '${show.screen}' from '${show.element}.selection', but has no table or component '${show.element}'`, show.location);
        return;
    }
    const keys = [...screenDirectives(target.directives)].filter(element => element.kind === 'ScreenDataSyntax').map(data => data.by).filter(key => key !== null);
    if (keys.length > 0 && !keys.includes(show.by)) context.error(DiagnosticCodes.InvalidScreenShow, `Screen '${screen.name}' shows '${show.screen}' by '${show.by}', but '${show.screen}' is keyed by ${keys.map(key => `'${key}'`).join(', ')}`, show.location);
}
