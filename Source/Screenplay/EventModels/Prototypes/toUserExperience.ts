// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ScreenDirectiveSyntax, ScreenSyntax } from '@cratis/screenplay-compiler';
import { ActorDocument, ActorType, PrototypeLayout, UserExperienceActorDocument } from '../Document/EventModelDocument';
import { guidFor } from '../Document/identity';
import { PrototypeCanvas } from './PrototypeCanvas';
import { layoutDirectives, prototypeMetrics } from './screenLayout';

// Whoever uses a screen no persona is said to - a model without personas, or a slice nobody is required
// to be authorized for. Their row of prototypes is drawn like a persona's.
export const userActor: ActorDocument = {
    id: guidFor('actor:User'),
    name: 'User',
    actorType: ActorType.uiRole,
    description: 'Whoever uses the screens of the model',
};

// What the actors who use a slice see - its screens, one under the other, drawn as one prototype in each of
// their rows. A slice without screens has none.
export function toUserExperience(screens: readonly ScreenSyntax[], path: string, actorIds: readonly string[] = [userActor.id]): UserExperienceActorDocument[] {
    if (screens.length === 0) {
        return [];
    }
    const { windowWidth, minimumWindowHeight, padding, gap } = prototypeMetrics;
    const canvas = new PrototypeCanvas(`${path}:prototype`);
    const region = { x: padding, width: windowWidth - padding * 2 };
    let y = padding;
    for (const screen of screens) {
        const height = screen.directives.length === 0
            ? placeExternal(canvas, screen, region, y)
            : layoutDirectives(canvas, screen.directives, region, y, presentsData(screen.directives));
        y += height + gap;
    }
    const windowHeight = Math.max(minimumWindowHeight, y - gap + padding);
    return actorIds.map(id => ({ id, type: ActorType.uiRole, elements: canvas.elements, layout: PrototypeLayout.absolute, windowWidth, windowHeight }));
}

// A screen implemented in a file says nothing about what is on it, so it is drawn as one content area.
function placeExternal(canvas: PrototypeCanvas, screen: ScreenSyntax, region: { x: number; width: number }, y: number): number {
    canvas.place('content-area', screen.name, region.x, y, region.width, prototypeMetrics.tableHeight);
    return prototypeMetrics.tableHeight;
}

// Whether the screen presents its data itself - with a table or a summary - so the data directive does not
// need a placeholder of its own.
function presentsData(directives: readonly ScreenDirectiveSyntax[]): boolean {
    return directives.some(directive => {
        switch (directive.kind) {
            case 'ScreenTableSyntax':
            case 'ScreenSummarySyntax':
                return true;
            case 'ScreenSectionSyntax':
                return presentsData(directive.directives);
            case 'ScreenTemplateReferenceSyntax':
                return directive.slots.some(slot => presentsData(slot.directives));
            default:
                return false;
        }
    });
}
