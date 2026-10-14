// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { generationChecksum } from './GenerationChecksum';
import { GenerationPattern } from './GenerationPattern';
import { expandToken } from './GenerationPatternToken';

/** Genuine least, greatest and alternating support samples; never fabricated date/checksum witnesses. */
export function finiteGenerationSamples(pattern: GenerationPattern, date?: Date): string[] {
    if (pattern.hasDates && date === undefined) return [];
    const samples: string[] = [];
    for (let choice = 0; choice < 3; choice++) {
        let payload = '';
        let position = 0;
        for (const token of pattern.tokens) {
            const repetitions = choice === 0 ? token.minimum : token.maximum;
            for (let repetition = 0; repetition < repetitions; repetition++) for (const characters of expandToken(token, date)) {
                payload += choice === 0 || (choice === 2 && position % 2 === 0) ? characters[0] : characters[characters.length - 1];
                position++;
            }
        }
        const suffix = pattern.checksum === undefined ? '' : generationChecksum(payload, pattern.checksum);
        if (suffix !== null) samples.push(payload + suffix);
    }
    return samples;
}
