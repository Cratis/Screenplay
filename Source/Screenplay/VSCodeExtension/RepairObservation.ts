// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import { createHash } from 'node:crypto';

/** Passive test diagnostics. No session references, tokens, contents or authority escape. */
export type RepairObservation = Readonly<{
    seq: number; at: number; source: string; owner?: number; generation?: number;
    currentGeneration?: number; owns?: boolean; retired?: boolean; epoch?: number;
    capturedEpoch?: number; disposed?: boolean; phase?: string; tokenHash?: string;
    proposalHash?: string; event?: string; filename?: string; scheme?: string;
    version?: number; dirty?: boolean; scope?: string; changes?: number; reason?: number;
    before?: number; after?: number;
}>;
const limit = 512;
const entries: RepairObservation[] = [];
let sequence = 0;
// Only a deliberately approved synthetic test scope opts in. Normal activation
// does no hashing, clock reads, buffering, I/O or logging through this sink.
const syntheticRoot = process.env.SCREENPLAY_REPAIR_OBSERVE_SYNTHETIC_ROOT;
export function repairObservationEnabled(root: string): boolean {
    return !!syntheticRoot && path.isAbsolute(syntheticRoot) && path.resolve(root) === path.resolve(syntheticRoot);
}
export function observationHash(value: string | undefined): string | undefined {
    return value === undefined ? undefined : createHash('sha256').update(value).digest('hex').slice(0, 16);
}
export function observationFilename(root: string, file: string | null): string | undefined {
    if (!repairObservationEnabled(root) || file === null) return undefined;
    const relative = path.isAbsolute(file) ? path.relative(root, file) : file;
    const normalized = relative.replaceAll('\\', '/');
    return normalized.length <= 200 && !normalized.split('/').some(part => part === '..') && !path.isAbsolute(normalized) ? normalized : undefined;
}
export function observeRepair(root: string, value: Omit<RepairObservation, 'seq' | 'at'>): void {
    if (!repairObservationEnabled(root)) return;
    entries.push(Object.freeze({ ...value, seq: ++sequence, at: Date.now() }));
    if (entries.length > limit) entries.shift();
}
/** Detached immutable copies only: reading never clears reviews or changes guards. */
export function readRepairObservation(): readonly RepairObservation[] {
    return Object.freeze(entries.map(entry => Object.freeze({ ...entry })));
}
