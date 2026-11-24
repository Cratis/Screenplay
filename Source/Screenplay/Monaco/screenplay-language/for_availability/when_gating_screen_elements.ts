// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { plainActionItemsFor } from '../completion-items';
import { keywordDocs } from '../keyword-docs';
import { validateLines } from '../validation';

const source = [
    'audience Staff',
    '',
    'module Billing',
    '    feature Invoices',
    '        slice StateChange Issue',
    '            event Issued',
    '                number String',
    '            command Issue',
    '                number String',
    '                produces Issued',
    '                    number = number',
    '            screen IssueScreen',
    '                action Issue',
    '                    label "Issue"',
    '                    available for Staff',
].join('\n').split('\n');

describe('when gating screen elements', () => {
    it('should not flag an available gate inside an action', () => {
        expect(validateLines(source).filter(issue => issue.line === 14)).toEqual([]);
    });

    it('should offer the gates inside a plain action', () => {
        expect(plainActionItemsFor('action Issue').map(item => item.label)).toEqual(expect.arrayContaining(['available for', 'available when', 'available with']));
    });

    it('should document that an element gate only narrows the bound part', () => {
        expect(keywordDocs.available).toContain('can only narrow');
    });
});
