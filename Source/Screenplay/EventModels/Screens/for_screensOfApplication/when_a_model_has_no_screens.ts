// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { noApplicationScreens, screensOfApplication } from '../screensOfApplication';
import { a_model } from './given/a_model';

describe('when a model has no screens', () => {
    const screens = screensOfApplication(a_model(
        'module Sales',
        '  feature Checkout',
        '    slice StateView Orders',
        '      query AllOrders => Order[]'));

    it('should hand the board nothing to draw', () => screens.should.equal(noApplicationScreens));
});

describe('when a model has nothing at all', () => {
    it('should hand the board nothing to draw', () => screensOfApplication(a_model('')).should.equal(noApplicationScreens));
});
