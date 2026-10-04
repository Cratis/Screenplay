// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExactNumber, parseExactNumber } from './ExactNumber';

/** A malformed or noncanonical explicitly typed exact numeric value. */
export class InvalidExactNumber extends Error {}

/** Validates typed values again at programmatic boundaries; readonly interfaces alone are not admission. */
export function canonicalExactText(number: ExactNumber): string {
    if (number.literalType !== 'ExactNumber' || typeof number.value !== 'string'
        || parseExactNumber(number.value)?.value !== number.value) {
        throw new InvalidExactNumber('Expected a canonical, bounded ExactNumber value.');
    }
    return number.value;
}

export function exactEqual(left: ExactNumber, right: ExactNumber): boolean {
    return canonicalExactText(left) === canonicalExactText(right);
}

export function exactIsIntegral(number: ExactNumber): boolean {
    return !canonicalExactText(number).includes('.');
}

/** Compares bounded canonical decimal strings without JavaScript numeric conversion. */
export function exactCompare(left: ExactNumber, right: ExactNumber): number {
    const leftText = canonicalExactText(left);
    const rightText = canonicalExactText(right);
    if (leftText === rightText) return 0;
    const leftNegative = leftText.startsWith('-');
    const rightNegative = rightText.startsWith('-');
    if (leftNegative !== rightNegative) return leftNegative ? -1 : 1;
    const [leftInteger, leftFraction = ''] = (leftNegative ? leftText.substring(1) : leftText).split('.');
    const [rightInteger, rightFraction = ''] = (rightNegative ? rightText.substring(1) : rightText).split('.');
    const direction = leftNegative ? -1 : 1;
    if (leftInteger.length !== rightInteger.length) return (leftInteger.length < rightInteger.length ? -1 : 1) * direction;
    if (leftInteger !== rightInteger) return (leftInteger < rightInteger ? -1 : 1) * direction;
    const length = Math.max(leftFraction.length, rightFraction.length);
    return (leftFraction.padEnd(length, '0') < rightFraction.padEnd(length, '0') ? -1 : 1) * direction;
}
