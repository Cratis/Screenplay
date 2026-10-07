// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { timelineOrderDiagnostics } from '../../Files/TimelineOrder';
import { DependencyGraph } from '../DependencyGraph';

const source = 'module M\n  feature F\n    slice StateView V\n      projection P\n        from Z, A\n    slice StateChange W\n      event Z\n      event A\n';

describe('when references share a source location', () => {
    let events: string[];
    let graph: DependencyGraph;
    beforeEach(() => {
        const application = parse(source).value;
        events = timelineOrderDiagnostics(application).map(finding => /uses event '([^']+)'/.exec(finding.message)![1]);
        graph = DependencyGraph.for(application);
    });
    it('should preserve timeline reference order', () => {
        events.should.deep.equal(['Z', 'A']);
    });
    it('should sort graph evidence ties ordinally', () => {
        graph.edges[0].evidence.map(item => item.name).should.deep.equal(['A', 'Z']);
    });
});
