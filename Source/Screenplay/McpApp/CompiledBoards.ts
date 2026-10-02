// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompiledBoard } from './CompiledBoard';

// The application as it is, and - when a change is shown - as the change would leave it.
export interface CompiledBoards {
    readonly current: CompiledBoard;
    readonly proposed?: CompiledBoard;
}
