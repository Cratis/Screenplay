// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMap, DependencyOverlay, EventModelDocument, ScreenDocuments } from '@cratis/screenplay-event-models';
import type { ConversionFinding } from '@cratis/scene.screenplay';

// One model compiled for the board: the document it draws, the Scene documents of its screens, and how many
// errors compiling found. The board draws whatever could be read, so the errors say what may be missing.
export interface CompiledBoard {
    readonly document: EventModelDocument;
    readonly dependencies: DependencyMap;
    readonly overlay: DependencyOverlay;
    readonly errors: number;

    // The Scene document of every screen, by the key the board draws it under.
    readonly screens: ScreenDocuments;

    // What a screen said that could not be drawn. Never an error: that role keeps the placeholder window.
    readonly screenFindings: readonly ConversionFinding[];
}
