// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { diagnosticCodes } from '../diagnostic-codes';
import { validateLines } from '../validation';

const names = Array.from({ length: 21 }, (_, index) => `All${index}`);
const model = [
    'policy Staff', '  require role "Staff"', 'module Sales', '  authorize Staff', '  feature Listing',
    '    slice StateView List', '      readmodel Invoice', '        id String',
    ...names.map(name => `      query ${name} => Invoice[]`),
];
const agents = ['agents', '  tokens personal', ...names.flatMap(name => [`  query ${name}`, '    scope read', '    description "Read invoices"'])];

describe('when authoring a large agent allowlist', () => {
    it('should forward the compiler count warning at the agents block', () => {
        const warnings = validateLines([...model, ...agents]).filter(issue => issue.code === diagnosticCodes.agentToolCountExceedsRecommendation);
        warnings.should.have.lengthOf(1);
        (warnings[0].code === 'PLAY1167').should.equal(true);
        warnings[0].severity.should.equal('warning');
        warnings[0].line.should.equal(model.length);
        warnings[0].startColumn.should.equal(1);
        warnings[0].message.should.equal('The agents block exposes 21 operations; expose at most 20 focused operations to keep agent tool selection effective');
    });
});
