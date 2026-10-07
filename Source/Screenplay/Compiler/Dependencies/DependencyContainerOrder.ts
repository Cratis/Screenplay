// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyNode } from './DependencyNode';

export interface DependencyContainerOrder {
    readonly container: DependencyNode;
    readonly children: readonly DependencyNode[];
    readonly changed: boolean;
}
