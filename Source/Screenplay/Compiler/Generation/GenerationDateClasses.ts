// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { generationChecksum } from './GenerationChecksum';
import { GenerationPattern } from './GenerationPattern';
import { expandToken } from './GenerationPatternToken';
import { GenerationWorkBudget } from './GenerationWorkBudget';

const digit = (character: string) => character >= '0' && character <= '9';

export class GenerationDateClasses {
    private readonly fixedWidth: boolean;
    private readonly fixedDigitPositions: boolean;

    constructor(private readonly pattern: GenerationPattern) {
        this.fixedWidth = pattern.tokens.every(token => token.minimum === token.maximum);
        this.fixedDigitPositions = pattern.tokens.every(token => token.segment !== undefined ||
            [...token.characters].every(digit) || [...token.characters].every(character => !digit(character)));
    }

    key(date: Date, budget: GenerationWorkBudget): string {
        // Fixed character positions make date substitution bijective. Checksum grouping additionally
        // needs fixed ASCII digit positions: mixed digit/nondigit classes can change date weights.
        if (!this.fixedWidth || (this.pattern.checksum !== undefined && !this.fixedDigitPositions)) {
            return this.pattern.tokens.filter(token => token.segment !== undefined).map(token => expandToken(token, date).join('')).join('|');
        }
        if (this.pattern.checksum === undefined) return '';

        // Zero non-date digits but retain their digit positions. The mask's checksum is a bijective
        // fingerprint of date contribution (Luhn sum/parity mod10, mod11 right-hand weights 2..7,
        // mod97 decimal residue). At most 10/11/97 classes; '10' denotes excluded mod11 output.
        let mask = '';
        for (const token of this.pattern.tokens) {
            const digits = token.segment !== undefined ? expandToken(token, date).join('') : [...token.characters].every(digit) ? '0' : '';
            for (let repetition = 0; repetition < token.minimum; repetition++) {
                for (const character of digits) {
                    budget.spend(); // Inspect one fixed digit-position edge.
                    mask += character;
                }
            }
        }
        budget.spend(mask.length); // One checksum-state transition per digit actually processed.
        return generationChecksum(mask, this.pattern.checksum) ?? '10';
    }
}
