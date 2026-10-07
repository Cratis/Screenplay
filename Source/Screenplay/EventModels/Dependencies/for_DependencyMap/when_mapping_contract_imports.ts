// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { dependencyMapFor } from '../dependencyMapFor';
import { sampleApplication } from './given/a_sample';

describe('when mapping contract imports', () => {
    const map = dependencyMapFor(sampleApplication('Invoicing'));
    it('should include Customers Payments and Shipping as outer nodes', () => map.contexts.map(key => map.nodes.find(node => node.key === key)!.scope[0]).sort().should.deep.equal(['Customers', 'Payments', 'Shipping']));
    it('should connect consumers to the outer contexts', () => map.contexts.every(key => map.edges.some(edge => edge.target === key && edge.byKind.outsideTheModel! > 0)).should.be.true);
});
