// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { describePlacement, documentPlacement, isDocumentPlacement, isWithinOrSame, samePlacement } from '../PlayPlacement';

describe('when describing placements', () => {
    it('should describe a whole document as the application', () => {
        describePlacement(documentPlacement).should.equal('the application');
    });

    it('should describe a module by its name', () => {
        describePlacement(['Ordering']).should.equal('module \'Ordering\'');
    });

    it('should describe a feature by its full name', () => {
        describePlacement(['Ordering', 'Orders', 'Drafts']).should.equal('feature \'Ordering.Orders.Drafts\'');
    });

    it('should tell a whole document from a placed one', () => {
        [isDocumentPlacement([]), isDocumentPlacement(['Ordering'])].should.deep.equal([true, false]);
    });

    it('should see a deeper placement inside a shallower one', () => {
        isWithinOrSame(['Ordering', 'Orders'], ['Ordering']).should.be.true;
    });

    it('should not see a shallower placement inside a deeper one', () => {
        isWithinOrSame(['Ordering'], ['Ordering', 'Orders']).should.be.false;
    });

    it('should not see placements apart inside each other', () => {
        isWithinOrSame(['Billing', 'Orders'], ['Ordering']).should.be.false;
    });

    it('should tell the same placement from another', () => {
        [samePlacement(['Ordering'], ['Ordering']), samePlacement(['Ordering'], ['Ordering', 'Orders'])].should.deep.equal([true, false]);
    });
});
