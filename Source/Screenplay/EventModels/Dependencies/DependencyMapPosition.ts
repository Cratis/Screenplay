// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMapNode } from './DependencyMapNode';

export interface DependencyMapPosition extends DependencyMapNode {
    readonly x: number;
    readonly y: number;
    readonly width: number;
    readonly height: number;
}
