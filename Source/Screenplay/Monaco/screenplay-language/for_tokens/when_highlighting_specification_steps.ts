// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ensureBuiltInSubLanguages } from '../index';
import { getSubLanguages } from '../sub-language-registry';
import { createTokensProvider } from '../tokens';

ensureBuiltInSubLanguages();
const rules = createTokensProvider(getSubLanguages()).tokenizer.root as unknown as [RegExp, unknown][];
const stepRules = rules.filter((rule) => Array.isArray(rule[1]) && (rule[1] as string[]).join() === 'white,keyword,white,keyword');
const captureLanguage = rules.findIndex((rule) => rule[0].source === '\\bcapture\\b');

describe('when highlighting specification steps', () => {
    it('should come before the capture sub-language can claim a step', () => {
        rules.indexOf(stepRules[stepRules.length - 1]).should.be.below(captureLanguage);
    });

    for (const line of ['    given clock "2026-10-05T08:00:00Z"', '    given capture LegacyLoans', '    when trigger NightlySync', '    when query OpeningHoursFor', '    then result exactly', '    then no result', '    then no events']) {
        it(`should read the step words of '${line.trim()}' as keywords`, () => {
            stepRules.some((rule) => rule[0].test(line)).should.be.true;
        });
    }

    it('should leave a capture declaration to the sub-language', () => {
        stepRules.some((rule) => rule[0].test('      capture LegacyLoans')).should.be.false;
    });
});
