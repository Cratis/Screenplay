// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { parseSpecificationSource } from '../ScreenplayCompiler';

describe('when parsing specification event streams', () => {
    let result: ReturnType<typeof parseSpecificationSource>;
    beforeEach(() => {
        result = parseSpecificationSource(`specification Routed
  given E
    for "other"
    stream Account.Transactions
      streamId = "p-1:2026-10"
    stream = 5
  when append E
    for "other"
    stream Account.Profile
  then E
    no stream
    streamId = 6`);
    });
    it('should accept event routes', () => result.diagnostics.should.deep.equal([]));
    it('should read the keyed route', () => result.value[0].given[0].stream!.stream.should.equal('Transactions'));
    it('should read the unkeyed route', () => result.value[0].whenAppended!.stream!.stream.should.equal('Profile'));
    it('should read the unrouted marker', () => result.value[0].thenEvents[0].noStream!.kind.should.equal('SpecificationNoStreamSyntax'));
    it('should keep stream as payload', () => result.value[0].given[0].values.map(value => value.property).should.deep.equal(['stream']));
    it('should keep stream id as payload', () => result.value[0].thenEvents[0].values.map(value => value.property).should.deep.equal(['streamId']));
});
