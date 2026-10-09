// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';

const contracts = `import Outside.Arrived from "other/store"
module M
  feature F
    slice StateChange Facts
      event Changed
      public event Published
`;

describe('when an outbound translation has a conditional production without an event', () => {
    let result: ReturnType<typeof parse>;

    beforeEach(() => {
        result = parse(contracts + '    slice Translate Send\n      direction outbound\n      reaction Send\n        when Changed\n          produces when amount > 0\n');
    });

    it('should reject the incomplete production', () => {
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([
            DiagnosticCodes.ProducesWhenWithoutEvent,
            DiagnosticCodes.OutboundPublicEventCount,
        ]);
    });
    it('should not count the incomplete edge as a public output', () => {
        result.value.modules[0].features[0].slices[1].reactions[0].triggers[0].produces.should.deep.equal([]);
        result.success.should.be.false;
    });
});

for (const [kind, direction, expected] of [
    ['StateChange', '', [DiagnosticCodes.CommandProducesPublicEvent, DiagnosticCodes.PublicEventRequiresOutboundTranslation]],
    ['Translate', 'inbound', [DiagnosticCodes.PublicEventRequiresOutboundTranslation, DiagnosticCodes.InboundTranslationOutput]],
    ['Translate', 'outbound', []],
] as const) {
    describe(`when conditionally producing a public event from ${direction || 'command'} ${kind}`, () => {
        let result: ReturnType<typeof parse>;

        beforeEach(() => {
            const action = kind === 'StateChange' ? 'command Send\n        amount number' : `reaction Send\n        when ${direction === 'inbound' ? 'Arrived' : 'Changed'}`;
            const indentation = kind === 'StateChange' ? '        ' : '          ';
            result = parse(contracts + `    slice ${kind} Send\n${direction ? `      direction ${direction}\n` : ''}      ${action}\n${indentation}produces when amount > 0\n${indentation}  Published\n`);
        });

        it('should apply the public production boundary even to conditional outputs', () => {
            result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(expected);
        });
        it('should accept only the explicitly outbound public production', () => {
            result.success.should.equal(direction === 'outbound');
        });
    });
}
