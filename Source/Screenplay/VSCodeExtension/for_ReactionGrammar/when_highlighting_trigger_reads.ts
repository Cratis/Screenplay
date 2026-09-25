// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));

describe('when highlighting reads under a reaction trigger', () => {
    it('recognizes reads as a directive', () => {
        grammar.repository.keywords.patterns.some(
            (pattern: { match?: string; name?: string }) =>
                pattern.name === 'keyword.other.screenplay' &&
                new RegExp(pattern.match!).test('reads Status as current by key'),
        ).should.be.true;
    });

    it('recognizes @reads as an escaped value before matching keywords', () => {
        const escaped = grammar.patterns.findIndex((pattern: { include?: string }) => pattern.include === '#escaped-name');
        const keywords = grammar.patterns.findIndex((pattern: { include?: string }) => pattern.include === '#keywords');
        escaped.should.be.lessThan(keywords);
        new RegExp(grammar.repository['escaped-name'].match).test('    @reads String').should.be.true;
    });
});
