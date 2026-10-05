// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const date = /^([0-9]{4})-([0-9]{2})-([0-9]{2})$/;
const dateTime = /^([0-9]{4})-([0-9]{2})-([0-9]{2})T([0-9]{2}):([0-9]{2}):([0-9]{2})(?:\.[0-9]{1,7})?(?:Z|[+-]([0-9]{2}):([0-9]{2}))$/;

// Only response/fixture compatibility uses this grammar; general literal parsing stays unchanged.
export function responseDateValue(text: string, includeTime: boolean): boolean {
    const match = (includeTime ? dateTime : date).exec(text);
    if (match === null || match[0].length !== text.length) return false;
    const year = Number(match[1]);
    const month = Number(match[2]);
    const day = Number(match[3]);
    const leap = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
    const days = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    if (year < 1 || month < 1 || month > 12 || day < 1 || day > days[month - 1]) return false;
    if (!includeTime) return true;
    if (Number(match[4]) > 23 || Number(match[5]) > 59 || Number(match[6]) > 59) return false;
    if (match[7] === undefined) return true;
    const hours = Number(match[7]);
    const minutes = Number(match[8]);
    return hours <= 14 && minutes <= 59 && (hours !== 14 || minutes === 0);
}
