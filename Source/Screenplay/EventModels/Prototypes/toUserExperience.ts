// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ScreenSyntax } from '@cratis/screenplay-compiler';
import { ActorDocument, ActorType, PrototypeLayout, UserExperienceActorDocument } from '../Document/EventModelDocument';
import { guidFor } from '../Document/identity';

// Whoever uses a screen no persona is said to - a model without personas, or a slice nobody is required
// to be authorized for. Their row of screens is drawn like a persona's.
export const userActor: ActorDocument = {
    id: guidFor('actor:User'),
    name: 'User',
    actorType: ActorType.uiRole,
    description: 'Whoever uses the screens of the model',
};

// What the actors who use a slice have: a screen each. The board draws nothing of a screen itself - a host that
// can draw one hands it the Scene document, and otherwise the board draws a placeholder window - so the document
// says only that there is one. A slice without screens has no row at all.
//
// A .play document never sizes the window a screen was designed in, so the window is left unsaid (0) and whoever
// draws the screen uses its own default, exactly as a Studio role whose window was never resized does.
export function toUserExperience(screens: readonly ScreenSyntax[], path: string, actorIds: readonly string[] = [userActor.id]): UserExperienceActorDocument[] {
    if (screens.length === 0) {
        return [];
    }

    void path;
    return actorIds.map(id => ({ id, type: ActorType.uiRole, hasScreen: true, layout: PrototypeLayout.absolute, windowWidth: 0, windowHeight: 0 }));
}
