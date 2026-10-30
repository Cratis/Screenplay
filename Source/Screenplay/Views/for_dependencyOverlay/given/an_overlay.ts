// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyOverlay } from '@cratis/screenplay-event-models';

// a consumes b and c; b consumes c. Selecting a: b and c are dependencies. Selecting b: a is a dependant, c a dependency.
// Selecting c: a and b are dependants. 'x' is unrelated, and 'm' holds a, b and c.
export const overlay: DependencyOverlay = {
    scopes: [
        { id: 'a', kind: 'slice', selected: ['a'], dependencies: ['b', 'c'], dependants: [], ancestors: ['m'], links: [{ from: 'a', to: 'b', crossing: false }, { from: 'a', to: 'c', crossing: true }, { from: 'b', to: 'c', crossing: false }] },
        { id: 'b', kind: 'slice', selected: ['b'], dependencies: ['c'], dependants: ['a'], ancestors: ['m'], links: [{ from: 'a', to: 'b', crossing: false }, { from: 'b', to: 'c', crossing: false }] },
        { id: 'both', kind: 'slice', selected: ['both'], dependencies: ['b'], dependants: ['b'], ancestors: [], links: [] },
    ],
};
