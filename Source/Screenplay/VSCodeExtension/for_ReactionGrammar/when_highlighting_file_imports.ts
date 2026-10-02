// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; captures?: Record<string, { name: string }> }[];
const fileImport = rules.find(rule => rule.captures?.['2']?.name === 'string.quoted.double.import-path.screenplay')!;

describe('when highlighting file imports', () => {
    it('scopes the quoted path of an import at any indentation', () => {
        new RegExp(fileImport.match).exec('    import "Orders/**/*.play"')![2].should.equal('"Orders/**/*.play"');
    });

    it('leaves a qualified import to the keyword rule', () => {
        new RegExp(fileImport.match).test('import Customers.CustomerRegistered').should.be.false;
    });
});
