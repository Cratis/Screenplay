// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ShownModel } from './ShownModel';

// What the board was last drawn from: which model, and which document of it.
export interface DrawnBoard {
    readonly shown: ShownModel;
    readonly document: object;
}

// The element to scroll to after the board has drawn `current`, or undefined to leave the board where it is.
// The first draw opens at the first change by itself (`initialFocus`). A different document under the same
// board - a switch between Current and Proposed, or the watcher handing over a new tool result - keeps the
// old scroll position, so go to the first change of the one now shown.
export function boardFocus(previous: DrawnBoard | undefined, current: DrawnBoard, first: string | undefined): string | undefined {
    if (previous === undefined || first === undefined) return undefined;
    return previous.shown !== current.shown || previous.document !== current.document ? first : undefined;
}
