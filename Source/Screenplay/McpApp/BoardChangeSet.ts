// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { BoardChange } from '@cratis/event-models';

// What the board marks on one drawn document, and where it opens.
export interface BoardChangeSet {
    // The marked elements by id.
    readonly changes: ReadonlyMap<string, BoardChange>;
    // The first marked element in board order; undefined when nothing is marked.
    readonly first?: string;
    readonly added: number;
    readonly changed: number;
    readonly removed: number;
}
