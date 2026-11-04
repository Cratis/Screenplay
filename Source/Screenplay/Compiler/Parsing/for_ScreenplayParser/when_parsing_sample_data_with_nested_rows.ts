// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

// Sample packs are intentionally opaque to the TypeScript compiler.
describe('when parsing sample data with nested rows', () => {
    const parsed = parse(`sample data Demo
  shape Basket
    row
      lines
        row
          sku = "BOOK"
          children
            row
              sku = "PEN"
    row
      lines

module Shop
  feature Baskets
    slice StateView BasketList
      readmodel Basket
        name String`);

    it('should skip the entire pack without diagnostics', () => parsed.diagnostics.should.be.empty);
    it('should succeed', () => parsed.success.should.be.true);
    it('should still parse the following declaration', () => parsed.value.modules[0].name.should.equal('Shop'));
});
