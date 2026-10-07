// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyNode } from '@cratis/screenplay-compiler';
import { SliceScope } from '../Mapping/SliceScope';

export function boardIdOf(node: DependencyNode): string | undefined {
    if (node.kind === 'context' || node.kind === 'application') return undefined;
    let scope = SliceScope.module(node.scope[0]);
    for (const [index, name] of node.scope.slice(1).entries()) {
        scope = node.kind === 'slice' && index === node.scope.length - 2 ? scope.slice(name) : scope.feature(name);
    }
    return scope.id;
}
