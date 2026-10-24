// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, it } from 'vitest';
import { cdl } from '../sub-languages/cdl';

describe('when marking a capture key as the event source', () => {
    const pattern = (cdl.tokens[0] as unknown as [RegExp, string])[0];
    const grammar = JSON.parse(readFileSync(resolve(__dirname, '../../../VSCodeExtension/syntaxes/screenplay.tmLanguage.json'), 'utf8'));
    const captureKeywords: string = grammar.repository['capture-block'].patterns[0].match;

    it('should tokenize is as a Monaco keyword', () => pattern.test('is').should.be.true);
    it('should tokenize event as a Monaco keyword', () => pattern.test('event').should.be.true);
    it('should match is in the VS Code capture grammar', () => new RegExp(captureKeywords).test('is').should.be.true);
    it('should match event in the VS Code capture grammar', () => new RegExp(captureKeywords).test('event').should.be.true);
});
