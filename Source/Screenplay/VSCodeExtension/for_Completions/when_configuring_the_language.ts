// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { blockHeaderPattern } from '@cratis/screenplay-language';

const configuration = JSON.parse(readFileSync(new URL('../language-configuration.json', import.meta.url), 'utf8'));

describe('when configuring the language', () => {
    it('should indent after the same block headers the Monaco language service does', () => {
        configuration.onEnterRules[0].beforeText.should.equal(blockHeaderPattern.source);
    });

    it('should indent after a command header but not after a complete line', () => {
        const pattern = new RegExp(configuration.onEnterRules[0].beforeText);
        pattern.test('      command AddProduct').should.be.true;
        pattern.test('  concept Sku : String').should.be.false;
    });
});
