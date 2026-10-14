// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChecksumAlgorithm } from './ChecksumAlgorithm';
import { checksumWidth } from './GenerationChecksum';
import { GenerationPatternParser } from './GenerationPatternParser';
import { GenerationPatternToken, tokenWidth } from './GenerationPatternToken';
import { InvalidGenerationPattern } from './InvalidGenerationPattern';

export interface GenerationPattern {
    readonly tokens: readonly GenerationPatternToken[];
    readonly checksum?: ChecksumAlgorithm;
    readonly checksumWidth: number;
    readonly minimumLength: number;
    readonly maximumLength: number;
    readonly hasDates: boolean;
}
export function parseGenerationPattern(pattern: string, checksum?: ChecksumAlgorithm): GenerationPattern {
    const tokens = new GenerationPatternParser(pattern).parse();
    const width = checksum === undefined ? 0 : checksumWidth(checksum);
    const minimumLength = tokens.reduce((sum, token) => sum + tokenWidth(token) * token.minimum, width);
    const maximumLength = tokens.reduce((sum, token) => sum + tokenWidth(token) * token.maximum, width);
    if (maximumLength > 256) throw new InvalidGenerationPattern('final expanded length exceeds 256 characters including check digits', pattern.length);
    return { tokens, checksum, checksumWidth: width, minimumLength, maximumLength, hasDates: tokens.some(token => token.segment !== undefined) };
}
