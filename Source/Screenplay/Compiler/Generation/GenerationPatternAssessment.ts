// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GenerationPattern } from './GenerationPattern';
import { GenerationDateClasses } from './GenerationDateClasses';
import { generationPatternCount } from './GenerationPatternSupport';
import { GenerationWorkBudget } from './GenerationWorkBudget';

export class EmptyGenerationSupport extends Error {
    constructor(date: Date | undefined, checksum: string | undefined) { super(`Generation support is empty${date === undefined ? '' : ' on UTC ' + date.toISOString().slice(0, 10)} with checksum '${checksum ?? 'none'}'.`); }
}

export function minimumGenerationPatternCount(pattern: GenerationPattern, utcYear: number, budget = new GenerationWorkBudget()): bigint {
    if (!pattern.hasDates) {
        const count = generationPatternCount(pattern, undefined, budget);
        if (count === 0n) throw new EmptyGenerationSupport(undefined, pattern.checksum);
        return count;
    }
    if (!Number.isInteger(utcYear) || utcYear < 1 || utcYear > 9899) throw new Error('Generation assessment year must allow the complete 100-year UTC horizon.');
    const date = new Date(0);
    date.setUTCFullYear(utcYear, 0, 1);
    const counts = new Map<string, bigint>();
    const classes = new GenerationDateClasses(pattern);
    let minimum: bigint | undefined;
    while (date.getUTCFullYear() <= utcYear + 100) {
        budget.spend();
        const key = classes.key(date, budget);
        let count = counts.get(key);
        if (count === undefined) { count = generationPatternCount(pattern, date, budget); counts.set(key, count); }
        if (count === 0n) throw new EmptyGenerationSupport(date, pattern.checksum);
        if (minimum === undefined || count < minimum) minimum = count;
        date.setUTCDate(date.getUTCDate() + 1);
    }
    return minimum!;
}
