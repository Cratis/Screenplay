// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyEvidence, DependencyKind } from '@cratis/screenplay-compiler';

export interface DependencyMapEdge {
    readonly id: string;
    readonly source: string;
    readonly target: string;
    readonly byKind: Partial<Record<DependencyKind, number>>;
    readonly sliceEdges: number;
    readonly references: number;
    readonly evidence: readonly DependencyEvidence[];
    readonly crossing: boolean;
}
