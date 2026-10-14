// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RegexStateBudgetExceeded } from './RegexProofErrors';

/** Preflight even outside-fragment groups before recursive descent or any runtime assessment. */
export function validateRegexNesting(pattern: string): void {
    let depth = 0;
    let inClass = false;
    for (let index = 0; index < pattern.length; index++) {
        const character = pattern[index];
        if (character === '\\') { index++; continue; }
        if (character === '[') inClass = true;
        else if (character === ']') inClass = false;
        else if (!inClass && character === '(' && ++depth > 64) throw new RegexStateBudgetExceeded('group nesting budget exceeded');
        else if (!inClass && character === ')' && depth > 0) depth--;
    }
}
