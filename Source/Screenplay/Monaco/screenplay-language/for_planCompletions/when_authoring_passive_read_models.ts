// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { clauseKeywords } from '../language';
import { scanDocument } from '../symbols';

const prefix = ['module Billing', '  feature Invoices', '    slice StateChange Register'];

describe('when authoring passive read models', () => {
    it('should offer the bare directive only in the readmodel body', () => {
        const lines = [...prefix, '      readmodel RegistrationState', '        '];
        const item = planCompletions(lines, 4, 8, scanDocument(lines)).entries.find(entry => entry.label === 'passive')!;
        item.insertText.should.equal('passive');
        item.documentation.should.contain('never inferred');
    });

    it.each(['command RegisterInvoice', 'event InvoiceRegistered', 'projection RegistrationHistory => RegistrationState'])('should not offer passive in %s', header => {
        const lines = [...prefix, `      ${header}`, '        '];
        planCompletions(lines, 4, 8, scanDocument(lines)).entries.some(entry => entry.label === 'passive').should.equal(false);
    });

    it('should explain passivity and the default in hover text', () => {
        const lines = [...prefix, '      readmodel RegistrationState', '        passive'];
        const text = hoverContent(lines, 4, 'passive', 9, 16)!;
        text.should.contain('never inferred');
        text.should.contain('PLAY0859');
        text.should.contain('Realization');
    });

    it('should highlight the keyword', () => clauseKeywords.includes('passive').should.equal(true));
});
