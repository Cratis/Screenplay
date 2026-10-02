// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { slicesShownFor } from './given/an_application';

describe('when the document is a feature file', () => {
    const files = {
        'application.play': ['domain Acme', 'import "Ordering/Ordering.play"'],
        'Ordering/Ordering.play': ['module Ordering', '  import "Orders/Orders.play"', '  import "Returns/Returns.play"'],
        'Ordering/Orders/Orders.play': ['feature Orders', '  import "*.play"'],
        'Ordering/Orders/PlaceOrder.play': ['slice StateChange PlaceOrder', '  event OrderPlaced', '    name String'],
        'Ordering/Returns/Returns.play': ['feature Returns', '  import "*.play"'],
        'Ordering/Returns/ReturnOrder.play': ['slice StateChange ReturnOrder', '  event OrderReturned', '    name String'],
    };

    it('should show the slices of that feature only', () =>
        slicesShownFor('Ordering/Orders/Orders.play', files)!.should.deep.equal(['Ordering.Orders.PlaceOrder']));
});
