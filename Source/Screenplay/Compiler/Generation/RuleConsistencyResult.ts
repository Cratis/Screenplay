// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RuleConsistencyStatus } from './RuleConsistencyStatus';

/** A version-independent proof outcome; only genuine generated values can be witnesses. */
export interface RuleConsistencyResult {
    readonly status: RuleConsistencyStatus;
    readonly witness: string | null;
    readonly reason: string | null;
}

export function proven(): RuleConsistencyResult {
    return { status: RuleConsistencyStatus.Proven, witness: null, reason: null };
}

export function refuted(witness: string): RuleConsistencyResult {
    return { status: RuleConsistencyStatus.Refuted, witness, reason: null };
}

export function unproven(reason: string): RuleConsistencyResult {
    return { status: RuleConsistencyStatus.Unproven, witness: null, reason };
}
