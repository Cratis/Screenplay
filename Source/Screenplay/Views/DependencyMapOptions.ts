// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyKind } from '@cratis/screenplay-compiler';
import { DependencyMapLevel } from './DependencyMapLevel';

// What the dependency map draws: the level the edges run between and the kinds of dependency shown.
export interface DependencyMapOptions {
    readonly level: DependencyMapLevel;
    readonly kinds: readonly DependencyKind[];
}

export const defaultDependencyMapOptions: DependencyMapOptions = {
    level: DependencyMapLevel.Module,
    kinds: ['usesFactsFrom', 'reactsTo', 'decidesFrom'],
};
