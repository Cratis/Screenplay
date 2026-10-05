// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A lossless typed literal slot. The value is canonical fixed-point text, never a JavaScript number. */
export interface ExactNumber {
    readonly literalType: 'ExactNumber';
    readonly value: string;
}

const maximumCoefficient = '79228162514264337593543950335';

interface ScannedNumber {
    readonly negative: boolean;
    readonly integer: string;
    readonly fraction: string;
    readonly exponent: number;
}

/** Reads a complete ASCII token in the normalized Decimal domain, without rounding or underflow. */
export function parseExactNumber(text: string): ExactNumber | undefined {
    const scanned = scan(text);
    if (scanned === undefined) return undefined;
    const digits = scanned.integer + scanned.fraction;
    let first = 0;
    while (first < digits.length && digits[first] === '0') first++;
    if (first === digits.length) return Object.freeze({ literalType: 'ExactNumber', value: '0' });
    let last = digits.length;
    while (digits[last - 1] === '0') last--;
    const scale = scanned.fraction.length - scanned.exponent - (digits.length - last);
    const appendedZeros = scale < 0 ? -scale : 0;
    if (scale > 28 || last - first + appendedZeros > maximumCoefficient.length) return undefined;
    // Only the bounded coefficient is materialized; the authored exponent never determines allocation.
    const coefficient = digits.substring(first, last) + '0'.repeat(appendedZeros);
    if (coefficient.length === maximumCoefficient.length && coefficient > maximumCoefficient) return undefined;
    const normalizedScale = Math.max(0, scale);
    const fixedPoint = normalizedScale === 0 ? coefficient
        : coefficient.length > normalizedScale
            ? `${coefficient.substring(0, coefficient.length - normalizedScale)}.${coefficient.substring(coefficient.length - normalizedScale)}`
            : `0.${'0'.repeat(normalizedScale - coefficient.length)}${coefficient}`;
    return Object.freeze({ literalType: 'ExactNumber', value: (scanned.negative ? '-' : '') + fixedPoint });
}

/** Recognizes the complete scalar grammar independently of representability. */
export function isExactNumberToken(text: string): boolean {
    return scan(text) !== undefined;
}

function scan(text: string): ScannedNumber | undefined {
    const negative = text.startsWith('-');
    const integerStart = negative ? 1 : 0;
    let position = integerStart;
    while (position < text.length && isDigit(text[position])) position++;
    if (position === integerStart) return undefined;
    const integer = text.substring(integerStart, position);
    let fraction = '';
    if (text[position] === '.') {
        const fractionStart = ++position;
        while (position < text.length && isDigit(text[position])) position++;
        if (position === fractionStart) return undefined;
        fraction = text.substring(fractionStart, position);
    }
    let exponent = 0;
    if (text[position] === 'e' || text[position] === 'E') {
        position++;
        const exponentNegative = text[position] === '-';
        if (text[position] === '+' || text[position] === '-') position++;
        const exponentStart = position;
        // Counts derived from this input cannot cancel an exponent beyond this bound.
        const limit = text.length + 29;
        while (position < text.length && isDigit(text[position])) {
            exponent = Math.min(limit, exponent * 10 + text.charCodeAt(position++) - 48);
        }
        if (position === exponentStart) return undefined;
        if (exponentNegative) exponent = -exponent;
    }
    return position === text.length ? { negative, integer, fraction, exponent } : undefined;
}

function isDigit(value: string): boolean {
    return value >= '0' && value <= '9';
}
