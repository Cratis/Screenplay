// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { DependencyGraph } from '@cratis/screenplay-compiler';
import { dependencyOverlayFor } from '../dependencyOverlayFor';
import type { DependencyOverlay } from '../DependencyOverlay';
import { sampleApplication } from '../for_DependencyMap/given/a_sample';

describe('when mapping an application that depends on imported contexts', () => {
    const application = sampleApplication('Invoicing');
    let overlay: DependencyOverlay;
    beforeAll(() => { overlay = dependencyOverlayFor(application); });

    it('should give contexts no scope', () => overlay.scopes.length.should.equal(DependencyGraph.for(application).nodes.filter(node => node.kind !== 'context' && node.kind !== 'application').length));
    it('should leave contexts out of every list and link', () => {
        const known = new Set(overlay.scopes.map(scope => scope.id));
        overlay.scopes.every(scope => [...scope.selected, ...scope.dependencies, ...scope.dependants, ...scope.ancestors, ...scope.links.flatMap(link => [link.from, link.to])].every(id => known.has(id))).should.be.true;
    });
});
