// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const source = `module M
  feature F
    slice StateChange Pricing
      command ChangePrice
        product Uuid identifier
        price Decimal
        produces PriceChanged
          for product
          price = price
      event PriceChanged
        price Decimal
    slice Translate Publication
      direction outbound
      public event PricePublished
        price Decimal
      projection Publisher => PricePublished
        from PriceChanged
          price = price
      specification Publishing
        parameter id Uuid
        case One id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        given PriceChanged
          for case.id
          price = 10.5
        then PricePublished
          for case.id
          price = 10.5`;
const typeDiagnostics = (text: string) => parse(text).diagnostics.filter(diagnostic => ['PLAY0587', 'PLAY0585'].includes(diagnostic.code));

describe('when checking published case parameter destinations', () => {
    it('should accept a compatible published event parameter', () => typeDiagnostics(source).should.deep.equal([]));
    it('should accept a compatible published event parameter from every fact', () => {
        const every = source.replace('from PriceChanged\n          price = price', 'every\n          price = 10.5');
        typeDiagnostics(every).should.deep.equal([]);
    });
    it('should type the published event parameter from every fact', () => {
        const incompatible = source
            .replace('from PriceChanged\n          price = price', 'every\n          price = 10.5')
            .replace('parameter id Uuid', 'parameter id Int')
            .replace('case One id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"', 'case One id = 1')
            .replace('given PriceChanged\n          for case.id', 'given PriceChanged\n          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"');
        typeDiagnostics(incompatible).map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0587']);
    });
    it('should type the published event parameter from its folded facts', () => {
        // Keep the given source literal so only the published event uses the incompatible parameter.
        const incompatible = source
            .replace('parameter id Uuid', 'parameter id Int')
            .replace('case One id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"', 'case One id = 1')
            .replace('given PriceChanged\n          for case.id', 'given PriceChanged\n          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"');
        typeDiagnostics(incompatible).map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0587']);
    });
});
