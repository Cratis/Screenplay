// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { constructKeywords } from '../language';
import { createTokensProvider } from '../tokens';

const provider = createTokensProvider([]);
const rules = provider.tokenizer.root as unknown as [RegExp, unknown][];
const identityRule = rules.find(rule => rule[0].source.includes('(runs)'));

describe('when highlighting a reaction identity', () => {
    it('should register runs as a construct', () => {
        constructKeywords.should.contain('runs');
    });

    it('should not reserve runs as a global keyword', () => {
        provider.keywords.should.not.contain('runs');
    });

    it('should read a reaction identity line as keywords', () => {
        identityRule![0].test('    runs as system role "Accountant"').should.be.true;
    });

    it('should leave a property called runs to the property rules', () => {
        identityRule![0].test('        runs Int').should.be.false;
    });
});
