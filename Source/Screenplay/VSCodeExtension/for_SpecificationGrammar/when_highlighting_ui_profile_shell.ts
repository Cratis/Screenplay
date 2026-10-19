// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; captures?: Record<string, { name: string }> }[];

function capturedBy(line: string, capture: string): string | undefined {
    for (const rule of rules) {
        const match = new RegExp(rule.match).exec(line);
        if (match && rule.captures?.[capture]) return match[Number(capture)];
    }
    return undefined;
}

describe('when highlighting the ui profile shell lines', () => {
    it('scopes the root template keyword', () => capturedBy('  root template blueprint Cratis.Default AppShell', '1')?.should.equal('root template'));
    it('scopes the option keyword', () => capturedBy('    option navigationMode slim', '1')?.should.equal('option'));
    it('scopes the appearance keyword', () => capturedBy('  appearance', '1')?.should.equal('appearance'));
    it('scopes the accent keyword', () => capturedBy('    accent "#4f46e5"', '1')?.should.equal('accent'));
    it('scopes the color scheme keyword', () => capturedBy('    color scheme dark', '1')?.should.equal('color scheme'));
    it('scopes the color scheme value', () => capturedBy('    color scheme dark', '2')?.should.equal('dark'));
    it('scopes the corners keyword', () => capturedBy('    corners round', '1')?.should.equal('corners'));
    it('scopes the font keyword', () => capturedBy('    font inter', '1')?.should.equal('font'));
    it('scopes the logo keyword and kind', () => capturedBy('    logo image "data:image/svg+xml;base64,AAAA"', '2')?.should.equal('image'));
    it('scopes the show name keyword', () => capturedBy('    show name false', '1')?.should.equal('show name'));
    it('scopes the new screens keyword', () => capturedBy('  new screens use template Sheet', '1')?.should.equal('new screens use template'));
    it('does not scope a property that merely starts with the word', () => (capturedBy('    optionality String', '1') === undefined).should.be.true);
});
