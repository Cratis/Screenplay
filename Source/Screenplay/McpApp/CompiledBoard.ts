// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { EventModelDocument } from '@cratis/screenplay-event-models';

// One model compiled for the board: the document it draws, and how many errors compiling found. The board
// draws whatever could be read, so the errors say what may be missing.
export interface CompiledBoard {
    readonly document: EventModelDocument;
    readonly errors: number;
}
