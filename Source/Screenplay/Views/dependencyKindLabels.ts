// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyKind } from '@cratis/screenplay-compiler';

export const dependencyKindLabels: Record<DependencyKind, string> = {
    usesFactsFrom: 'uses facts from',
    reactsTo: 'reacts to',
    decidesFrom: 'decides from',
    asks: 'asks',
    shows: 'shows',
    outsideTheModel: 'outside the model',
    verifiedWith: 'verified with',
};
