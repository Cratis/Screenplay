// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const identity = grammar.repository['identity-block'];
const detail = identity.patterns.find((rule: { match?: string }) => rule.match?.includes('claim|query'));

describe('when highlighting identity metadata', () => {
    it('should scope the top-level header as a keyword', () => {
        new RegExp(identity.begin).exec('identity')![2].should.equal('identity');
        identity.beginCaptures['2'].name.should.equal('keyword.control.screenplay');
    });
    it.each(['  department String optional from claim "department"', '  organization Organization optional from query MyOrganization by $identity.id'])('should scope typed detail sources in %s', line => {
        const match = new RegExp(detail.match).exec(line)!;
        match[3].should.equal('from');
        ['claim', 'query'].should.contain(match[4]);
        detail.captures['2'].name.should.equal('storage.type.screenplay');
    });
    it('should retain inline code and descriptions as owned blocks', () => {
        identity.patterns.map((rule: { include?: string }) => rule.include).should.include.members(['#fenced-csharp', '#description-block']);
    });
});
