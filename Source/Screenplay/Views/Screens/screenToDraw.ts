// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { screenDocumentFor, type ScreenDocuments } from '@cratis/screenplay-event-models';

// What one cell of the board draws: the screen's document when there is one for that cell, it can be read, and
// there is a runtime ready to draw it with - and nothing at all otherwise, which is what leaves the board's own
// placeholder window in place.
//
// Nothing here is a failure: a model compiled a moment ago, a screen the bridge could not convert and a runtime
// that is still being built all answer the same way, because a board that is always openable is worth more than
// one that reports why a single cell is thin. What could not be converted is said once, in the problems list.
export function screenToDraw<TDocument>(
    screens: ScreenDocuments,
    sliceId: string,
    actorId: string,
    read: (text: string | undefined) => TDocument | undefined,
    ready: boolean): TDocument | undefined {
    if (!ready) return undefined;
    return read(screenDocumentFor(screens, sliceId, actorId));
}
