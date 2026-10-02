// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { composedWithImports, mergedByFolder, slicesShownFor } from './given/an_application';

describe('when the document is a slice file', () => {
    it('should show only its slice when its feature imports it', () =>
        slicesShownFor('Ordering/Orders/CancelOrder.play', composedWithImports)!.should.deep.equal(['Ordering.Orders.CancelOrder']));

    it('should show only its slice when it restates its module and feature', () =>
        slicesShownFor('Ordering/Orders/CancelOrder.play', mergedByFolder)!.should.deep.equal(['Ordering.Orders.CancelOrder']));
});
