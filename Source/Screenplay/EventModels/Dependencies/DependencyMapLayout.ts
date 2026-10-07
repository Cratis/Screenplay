// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMapPosition } from './DependencyMapPosition';

export interface DependencyMapLayout {
    readonly width: number;
    readonly height: number;
    readonly nodes: readonly DependencyMapPosition[];
}
