// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const source = `eventsource Account
  identifier String
  stream Main
  stream Other
module M
  feature F
    slice StateChange Change
      command C
        id String identifier
        period Int
        stream Account.Main
        reads Row
          by
            id = id
            period = period
        produces event Changed
          stream Account.Other
          period Int = period
    slice StateView View
      readmodel Row
        id String key
        period Int key
      query Find => Row optional
        by
          id String
          period Int
      reducer Reduce => Row
        from Account.Other
        on Changed
          file Reducer.cs
    slice Automation React
      reaction R
        from Account.Other
        when Changed
`;
const slices = () => toEventModelDocument(parse(source).value, 'Keys').collections[0].modules[0].features[0].slices;

describe('when mapping production routes, observer filters and read-model keys', () => {
    it('should show each production override and named reads key', () => {
        expect(parse(source).diagnostics).toEqual([]);
        const details = slices()[0].command!.logicDescription;
        expect(details).toContain('Produces Changed: Account.Other');
        expect(details).toContain('Reads Row by id = id, period = period');
    });
    it('should mark key fields and include each query key part', () => {
        expect(slices()[1].readModel!.schema.properties).toMatchObject({ id: { title: 'id (key)' }, period: { title: 'period (key)' } });
        expect(slices()[1].queries[0].parameters.map(parameter => parameter.name)).toEqual(['id', 'period']);
    });
    it('should describe reaction and reducer observation', () => {
        expect(slices()[1].description).toContain('Reducer Reduce: Observes Account.Other');
        expect(slices()[2].automationTrigger!.description).toContain('Observes Account.Other');
    });
});
