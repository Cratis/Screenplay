// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMap, DependencyOverlay, EventModelDocument } from '@cratis/screenplay-event-models';
import type { BoardScreenDocuments } from '@cratis/screenplay-views';

// One model compiled for the board: the document it draws, the Scene document of every screen it can draw,
// and how many errors compiling found. The board draws whatever could be read, so the errors say what may be
// missing; a screen that could not be read back simply has no document and keeps the placeholder window.
export interface CompiledBoard {
    readonly document: EventModelDocument;
    readonly dependencies: DependencyMap;
    readonly overlay: DependencyOverlay;
    readonly screens: BoardScreenDocuments;
    readonly errors: number;
}
