// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyKind } from './DependencyKind';
import { DependencyNode } from './DependencyNode';
import { DependencyEvidence } from './DependencyEvidence';

export interface ImpliedDependency {
    readonly source: DependencyNode;
    readonly target: DependencyNode;
    readonly sliceEdges: number;
    readonly references: number;
    readonly byKind: Readonly<Partial<Record<DependencyKind, number>>>;
    readonly consumers: readonly DependencyNode[];
    readonly producers: readonly DependencyNode[];
    readonly evidence: readonly DependencyEvidence[];
    readonly evidenceCount: number;
    readonly evidenceTruncated: boolean;
}
