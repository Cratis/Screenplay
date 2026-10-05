// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { ExactNumber, isExactNumberToken, parseExactNumber } from '../ExactNumber';
import { canonicalExactText, exactCompare, exactEqual, exactIsIntegral, InvalidExactNumber } from '../ExactMathFacts';

interface Vectors {
    readonly cases: readonly { readonly token: string; readonly canonical: string | null; readonly isToken: boolean }[];
    readonly coefficients: readonly string[];
    readonly minimumScale: number;
    readonly maximumScale: number;
    readonly removableZeros: number;
    readonly comparisons: readonly { readonly left: string; readonly right: string; readonly order: number }[];
}

const vectors: Vectors = JSON.parse(readFileSync(new URL('../../Conformance/exact-number-codec.json', import.meta.url), 'utf8'));

function number(text: string): ExactNumber {
    const parsed = parseExactNumber(text);
    expect(parsed, text).not.toBeUndefined();
    return parsed!;
}

function fixedPoint(coefficient: string, scale: number): string {
    if (scale === 0) return coefficient;
    if (coefficient.length > scale) return `${coefficient.substring(0, coefficient.length - scale)}.${coefficient.substring(coefficient.length - scale)}`;
    return `0.${'0'.repeat(scale - coefficient.length)}${coefficient}`;
}

describe('when reading shared boundary vectors', () => {
    it.each(vectors.cases)('should preserve or refuse the complete token $token', ({ token, canonical, isToken }) => {
        expect(isExactNumberToken(token)).toBe(isToken);
        const parsed = parseExactNumber(token);
        expect(parsed?.value ?? null).toBe(canonical);
        if (parsed !== undefined) {
            expect(parsed.literalType).toBe('ExactNumber');
            expect(typeof parsed.value).toBe('string');
            expect(Object.isFrozen(parsed)).toBe(true);
            expect(canonicalExactText(parsed)).toBe(canonical);
        }
    });

    it('should normalize hundreds of shared signed coefficient scale and removable zero vectors', () => {
        let count = 0;
        for (const coefficient of vectors.coefficients) {
            for (let scale = vectors.minimumScale; scale <= vectors.maximumScale; scale++) {
                for (const sign of ['', '-']) {
                    const canonical = sign + fixedPoint(coefficient, scale);
                    for (const token of [canonical, `${sign}${coefficient}e-${scale}`, `${sign}000${coefficient}${'0'.repeat(vectors.removableZeros)}e-${scale + vectors.removableZeros}`]) {
                        const parsed = number(token);
                        expect(parsed.value, token).toBe(canonical);
                        expect(exactEqual(parsed, number(canonical))).toBe(true);
                        expect(exactIsIntegral(parsed)).toBe(scale === 0);
                        count++;
                    }
                }
            }
        }
        expect(count).toBeGreaterThan(690);
    });

    it.each(vectors.comparisons)('should compare $left and $right mathematically', ({ left, right, order }) => {
        const first = number(left);
        const second = number(right);
        expect(exactCompare(first, second)).toBe(order);
        expect(exactCompare(second, first)).toBe(order === 0 ? 0 : -order);
        expect(exactEqual(first, second)).toBe(order === 0);
    });

    it('should retain integrality above Int64 and distinguish a fraction rounded whole by Double', () => {
        expect(exactIsIntegral(number('9223372036854775808'))).toBe(true);
        expect(exactIsIntegral(number('1.0000000000000000000000000001'))).toBe(false);
    });

    it('should bound huge exponents and normalize long removable coefficients without rounding', () => {
        const exponent = '9'.repeat(10000);
        expect(parseExactNumber('1e' + exponent)).toBeUndefined();
        expect(parseExactNumber('1e-' + exponent)).toBeUndefined();
        expect(number('-0e' + exponent).value).toBe('0');
        expect(isExactNumberToken('1e' + exponent + 'foo')).toBe(false);
        expect(number('1' + '0'.repeat(10000) + 'e-10000').value).toBe('1');
        expect(number('0.' + '0'.repeat(10000) + '1e10001').value).toBe('1');
        expect(parseExactNumber('0.' + '0'.repeat(10000) + '1')).toBeUndefined();
    });

    it('should refuse noncanonical or malformed programmatic exact tags before comparison', () => {
        for (const value of ['1.0', '-0', '1e0', '1e29', '', '+1', '١']) {
            const forged: ExactNumber = { literalType: 'ExactNumber', value };
            expect(() => canonicalExactText(forged)).toThrow(InvalidExactNumber);
            expect(() => exactCompare(forged, number('1'))).toThrow(InvalidExactNumber);
            expect(() => exactEqual(number('1'), forged)).toThrow(InvalidExactNumber);
            expect(() => exactIsIntegral(forged)).toThrow(InvalidExactNumber);
        }
        const malformed = { literalType: 'ExactNumber', value: 1 } as unknown as ExactNumber;
        expect(() => canonicalExactText(malformed)).toThrow(InvalidExactNumber);
        expect(() => canonicalExactText({ literalType: 'Decimal', value: '1' } as unknown as ExactNumber)).toThrow(InvalidExactNumber);
    });
});
