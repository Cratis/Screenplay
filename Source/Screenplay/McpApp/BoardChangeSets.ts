// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { BoardChangeSet } from './BoardChangeSet';

// The marks for both documents: Proposed shows what is added and changed, Current shows what is removed.
export interface BoardChangeSets {
    readonly current: BoardChangeSet;
    readonly proposed: BoardChangeSet;
}
