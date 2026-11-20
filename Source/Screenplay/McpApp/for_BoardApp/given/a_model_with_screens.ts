// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { VisualizedModel } from '../../VisualizedModel';

const withScreen = `module Sales
  feature Checkout
    slice StateView Orders
      query AllOrders => Order[]
      screen Listing
        title "Orders"`;

const withUndrawableScreen = `module Sales
  feature Checkout
    slice StateView Orders
      query AllOrders => Order[]
      screen Listing`;

export const modelWithAScreen: VisualizedModel = { application: 'Sales', documents: [{ path: 'sales.play', source: withScreen }] };

export const modelWithAnUndrawableScreen: VisualizedModel = { application: 'Sales', documents: [{ path: 'sales.play', source: withUndrawableScreen }] };
