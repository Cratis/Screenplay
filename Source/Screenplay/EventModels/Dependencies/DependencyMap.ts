// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMapNode } from './DependencyMapNode';
import type { DependencyMapEdge } from './DependencyMapEdge';

/** Serializable presentation data, including uncapped evidence and identities shared with the board. */
export interface DependencyMap {
    readonly nodes: readonly DependencyMapNode[];
    readonly modules: readonly string[];
    readonly features: readonly string[];
    readonly contexts: readonly string[];
    readonly edges: readonly DependencyMapEdge[];
}
