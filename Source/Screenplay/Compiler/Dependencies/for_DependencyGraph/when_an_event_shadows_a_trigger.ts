// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { graphOf, producer } from './given/a_model';

describe('when an event shadows a trigger', () => {
    it('should prefer the slice event', () => {
        const graph = graphOf(`trigger E\n${producer}  feature B\n    slice Automation Consumer\n      reaction R\n        when E\n`);
        expect(graph.edges.find(edge => edge.kind === 'reactsTo')?.producer.address).toBe('M.A.Producer');
        expect(graph.excludedReferences).toBe(0);
    });
    it('should prefer an imported event and mark the import as used', () => {
        const graph = graphOf('import Outside.E\ntrigger E\nmodule M\n  feature F\n    slice Automation Consumer\n      reaction R\n        when E\n');
        expect(graph.edges.map(edge => [edge.kind, edge.producer.address])).toEqual([['outsideTheModel', 'context:Outside']]);
        expect(graph.excludedReferences).toBe(0);
        expect(graph.unusedImports).toEqual([]);
    });
});
