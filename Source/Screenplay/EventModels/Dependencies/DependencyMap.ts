// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMapNode } from './DependencyMapNode';
import type { DependencyMapEdge } from './DependencyMapEdge';
import type { DependencyMapEvidence } from './DependencyMapEvidence';

/** Serializable presentation data with a shared table of uncapped reference evidence. */
export interface DependencyMap {
    readonly nodes: readonly DependencyMapNode[];
    readonly modules: readonly string[];
    readonly features: readonly string[];
    readonly contexts: readonly string[];
    readonly edges: readonly DependencyMapEdge[];
    readonly evidence: readonly DependencyMapEvidence[];
}
