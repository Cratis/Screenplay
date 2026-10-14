// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { WorkspaceApplication } from '../WorkspaceApplication';

const source = 'module Billing\n  feature Invoices\n    slice StateView Lookup\n      readmodel Invoice\n        number String\n      query GetInvoice => Invoice optional\n        by number String\n      specification MalformedNumber\n        when query GetInvoice\n          number = "A"\n        then error';

describe('when validating query rejection specifications in the workspace', () => {
    it('should preserve one queried call and one error without a shape diagnostic', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', source);
        application.diagnosticsFor('application.play').filter(diagnostic => diagnostic.code === DiagnosticCodes.MismatchedSpecificationQueryResult).should.be.empty;
    });
    it('should report an explicit no-result conflict at the specification owner', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', source + '\n        then no result');
        application.diagnosticsFor('application.play').some(diagnostic => diagnostic.code === DiagnosticCodes.MismatchedSpecificationQueryResult && diagnostic.severity === 'error' && diagnostic.location.line === 8).should.be.true;
    });
});
