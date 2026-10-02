// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { VisualizedDocument } from './VisualizedDocument';

// What the visualize-model tool gives the board: the application's documents as they are, and - for a
// proposal or a sketch - the documents the change would replace, add or take away.
export interface VisualizedModel {
    readonly application: string;
    readonly documents: readonly VisualizedDocument[];
    readonly changes?: readonly VisualizedDocument[] | null;
    readonly proposalId?: string | null;
}
