// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface GenerationIntegerRange { readonly minimum: bigint; readonly maximum: bigint; readonly count: bigint; }

export function parseGenerationIntegerRange(range: string): GenerationIntegerRange {
    const match = /^(-?(?:0|[1-9][0-9]*))\.\.(-?(?:0|[1-9][0-9]*))$(?![\s\S])/.exec(range);
    if (match === null) throw new Error('Random generation requires an inclusive, ordered Int64 range.');
    const minimum = BigInt(match[1]);
    const maximum = BigInt(match[2]);
    if (minimum < -9223372036854775808n || maximum > 9223372036854775807n || minimum > maximum) throw new Error('Random generation requires an inclusive, ordered Int64 range.');
    return { minimum, maximum, count: maximum - minimum + 1n };
}
