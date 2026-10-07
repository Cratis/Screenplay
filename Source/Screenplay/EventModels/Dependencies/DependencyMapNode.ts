// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyNode } from '@cratis/screenplay-compiler';

export interface DependencyMapNode {
    readonly key: string;
    readonly kind: DependencyNode['kind'];
    readonly scope: readonly string[];
}
