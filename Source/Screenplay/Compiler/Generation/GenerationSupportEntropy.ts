// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function supportEntropyBits(count: bigint): number {
    if (count <= 0n) throw new Error('Entropy requires positive generation support.');
    const binary = count.toString(2);
    const prefix = binary.slice(0, 53);
    const leading = Number(BigInt('0b' + prefix));
    return binary.length - 1 + Math.log2(leading / 2 ** (prefix.length - 1));
}

export function hasSupportBits(count: bigint, bits: number): boolean {
    if (count <= 0n || !Number.isSafeInteger(bits) || bits < 0) throw new Error('Entropy thresholds require positive support and non-negative bits.');
    return bits <= count.toString(2).length && count >= (1n << BigInt(bits));
}
