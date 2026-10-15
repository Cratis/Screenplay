// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChecksumAlgorithm } from './ChecksumAlgorithm';

/** Bounded FIFO of successful immutable counts; no assessment time enters the model. */
export class GenerationAssessmentCache {
    private readonly entries = new Map<string, bigint>();

    constructor(private readonly capacity = 128) {
        if (!Number.isSafeInteger(capacity) || capacity < 1) throw new Error('Assessment cache capacity must be positive.');
    }

    get(pattern: string, checksum: ChecksumAlgorithm | undefined, year: number, assess: () => bigint): bigint {
        if (pattern.length > 4096) return assess();
        const key = JSON.stringify([pattern, checksum, year]);
        const existing = this.entries.get(key);
        if (existing !== undefined) return existing;
        const result = assess(); // Failures are never retained.
        if (this.entries.size === this.capacity) this.entries.delete(this.entries.keys().next().value!);
        this.entries.set(key, result);
        return result;
    }
}
