// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { composedWithImports, mergedByFolder, slicesShownFor } from './given/an_application';

const everySlice = ['Ordering.Orders.PlaceOrder', 'Ordering.Orders.CancelOrder', 'Ordering.Returns.ReturnOrder'];

describe('when the document is a module file', () => {
    it('should show what it imports into its features', () =>
        slicesShownFor('Ordering/Ordering.play', composedWithImports)!.should.have.members(everySlice));

    it('should show what other files place in the module it declares', () =>
        slicesShownFor('Ordering/Ordering.play', mergedByFolder)!.should.have.members(everySlice));
});
