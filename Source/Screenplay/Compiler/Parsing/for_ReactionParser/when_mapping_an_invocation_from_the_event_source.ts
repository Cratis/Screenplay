// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { should } from 'chai';
import { parse } from '../../ScreenplayCompiler';

should();

describe('when mapping an invocation from the event source', () => {
    it('should preserve both spellings in the shared conformance vector', () => {
        const result = parse(readFileSync(new URL('../../Conformance/reaction-event-source.play', import.meta.url), 'utf8'));
        result.diagnostics.should.be.empty;
        const mappings = result.value.modules[0].features[0].slices[1].reactions[0].triggers[0].invokes.flatMap(invocation => invocation.mappings ?? []);
        mappings.map(mapping => mapping.source.kind).should.deep.equal(['EventSourceIdExpressionSyntax', 'EventContextExpressionSyntax']);
    });

    for (const [source, kind] of [['$eventSourceId', 'EventSourceIdExpressionSyntax'], ['$eventContext.eventSourceId', 'EventContextExpressionSyntax']]) {
        it(`should preserve ${source} as a typed expression`, () => {
            const result = parse(`module Orders
  feature Fulfilment
    slice Automation StartPicking
      command StartPicking
        orderId String identifier
      event OrderPlaced
      reaction PickOnPlacement
        when OrderPlaced
          invokes StartPicking
            orderId = ${source}`);
            result.diagnostics.should.be.empty;
            const invocation = result.value.modules[0].features[0].slices[0].reactions[0].triggers[0].invokes[0];
            invocation.mappings![0].source.kind.should.equal(kind);
        });
    }
});
