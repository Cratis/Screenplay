// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { createTokensProvider } from '../tokens';

const rules = createTokensProvider([]).tokenizer.root as unknown as [RegExp, unknown][];
const fileImportRule = rules.find((rule) => Array.isArray(rule[1]) && (rule[1] as string[]).includes('string.link'));

describe('when highlighting a file import', () => {
    it('should read the quoted path as a link', () => {
        fileImportRule![0].test('  import "Orders/**/*.play"').should.be.true;
    });

    it('should leave a qualified import to the keyword rules', () => {
        fileImportRule![0].test('import Customers.CustomerRegistered').should.be.false;
    });
});
