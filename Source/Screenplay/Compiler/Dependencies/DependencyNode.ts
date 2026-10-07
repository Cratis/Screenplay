// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyLevel } from './DependencyLevel';

export interface DependencyNode {
    readonly kind: DependencyLevel;
    readonly address: string;
    readonly scope: readonly string[];
    readonly rank: number;
    readonly key: string;
}
