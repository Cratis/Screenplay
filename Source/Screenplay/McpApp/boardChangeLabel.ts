// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { BoardChangeSet } from './BoardChangeSet';

// The toggle's text for one model with what is marked on it in words, so it is not told by colour alone:
// "Proposed · 3 added, 1 changed", "Current · 2 removed". Nothing marked leaves the name alone.
export function boardChangeLabel(name: string, set: BoardChangeSet): string {
    const parts = [
        set.added > 0 ? `${set.added} added` : undefined,
        set.changed > 0 ? `${set.changed} changed` : undefined,
        set.removed > 0 ? `${set.removed} removed` : undefined,
    ].filter(part => part !== undefined);
    return parts.length === 0 ? name : `${name} · ${parts.join(', ')}`;
}
