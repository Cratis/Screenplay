// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChecksumAlgorithm } from './ChecksumAlgorithm';
import { checksumWidth } from './GenerationChecksum';
import { parseGenerationPattern } from './GenerationPattern';
import { proven, RuleConsistencyResult, unproven } from './RuleConsistencyResult';

/** Construction proof, independent of syntax binding: a different or absent checksum has no invented counterexample. */
export function checkGeneratedChecksum(pattern: string, checksum: ChecksumAlgorithm | undefined, algorithm: ChecksumAlgorithm): RuleConsistencyResult {
    const generation = parseGenerationPattern(pattern, checksum);
    checksumWidth(algorithm);
    return generation.checksum === algorithm ? proven() : unproven('checksum arithmetic requires a conditioned-support proof');
}
