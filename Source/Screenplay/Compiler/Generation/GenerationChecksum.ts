// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChecksumAlgorithm } from './ChecksumAlgorithm';

export function checksumWidth(algorithm: ChecksumAlgorithm): number {
    switch (algorithm) {
        case ChecksumAlgorithm.Luhn: case ChecksumAlgorithm.Mod11: return 1;
        case ChecksumAlgorithm.Mod97: return 2;
        default: throw new Error('Checksum requires luhn, mod11 or mod97.');
    }
}

export function generationChecksum(payload: string, algorithm: ChecksumAlgorithm): string | null {
    checksumWidth(algorithm);
    if (!printable(payload)) throw new Error('Checksum payload must contain printable ASCII characters.');
    const digits = [...payload].filter(character => character >= '0' && character <= '9').map(character => character.charCodeAt(0) - 48);
    const value = algorithm === ChecksumAlgorithm.Luhn ? luhn(digits) : algorithm === ChecksumAlgorithm.Mod11 ? mod11(digits) : mod97(digits);
    return algorithm === ChecksumAlgorithm.Mod11 && value === 10 ? null : value.toString().padStart(checksumWidth(algorithm), '0');
}

export function matchesChecksum(value: string, algorithm: ChecksumAlgorithm): boolean {
    const width = checksumWidth(algorithm);
    if (value.length < width || !printable(value)) return false;
    const suffix = value.slice(-width);
    if (![...suffix].every(character => character >= '0' && character <= '9')) return false;
    return suffix === generationChecksum(value.slice(0, -width), algorithm);
}

function printable(value: string): boolean {
    return [...value].every(character => character >= ' ' && character <= '~');
}

function luhn(digits: number[]): number {
    let sum = 0;
    for (let index = 0; index < digits.length; index++) {
        const digit = digits[digits.length - index - 1] * (index % 2 === 0 ? 2 : 1);
        sum = (sum + (digit > 9 ? digit - 9 : digit)) % 10;
    }
    return (10 - sum % 10) % 10;
}

function mod11(digits: number[]): number {
    let sum = 0;
    for (let index = 0; index < digits.length; index++) sum = (sum + digits[digits.length - index - 1] * (2 + index % 6)) % 11;
    return (11 - sum % 11) % 11;
}

function mod97(digits: number[]): number {
    let residue = 0;
    for (const digit of digits) residue = (residue * 10 + digit) % 97;
    return 98 - residue * 100 % 97;
}
