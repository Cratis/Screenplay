// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyNode } from './DependencyNode';

export interface SiblingEdge {
    readonly container: DependencyNode;
    readonly source: DependencyNode;
    readonly target: DependencyNode;
}
