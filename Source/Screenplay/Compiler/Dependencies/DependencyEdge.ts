// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyNode } from './DependencyNode';
import { DependencyKind } from './DependencyKind';
import { DependencyEvidence } from './DependencyEvidence';

export interface DependencyEdge {
    readonly consumer: DependencyNode;
    readonly producer: DependencyNode;
    readonly kind: DependencyKind;
    readonly evidence: readonly DependencyEvidence[];
}
