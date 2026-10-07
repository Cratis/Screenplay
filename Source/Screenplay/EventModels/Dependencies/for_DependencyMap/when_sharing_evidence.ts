// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { dependencyMapFor } from '../dependencyMapFor';
import { sampleApplication } from './given/a_sample';

describe('when sharing dependency evidence across aggregations', () => {
    const map = dependencyMapFor(sampleApplication('TimeTracking'));
    it('should store each reference once', () => new Set(map.evidence.map(item => JSON.stringify(item))).size.should.equal(map.evidence.length));
    it('should reference the shared table from every edge', () => map.edges.every(edge => edge.evidence.every(id => Number.isInteger(id) && map.evidence[id] !== undefined)).should.be.true);
    it('should reuse evidence between module and feature edges', () => map.edges.reduce((count, edge) => count + edge.evidence.length, 0).should.be.greaterThan(map.evidence.length));
    it('should not embed full graph nodes in the payload', () => map.evidence.every(item => typeof item.consumer === 'string' && typeof item.producer === 'string').should.be.true);
    it('should omit graph metadata unused by the view', () => map.nodes.every(node => !('rank' in node) && !('location' in node) && !('address' in node)).should.be.true);
});
