// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, expect, it, vi } from 'vitest';
import * as path from 'node:path';

const root = path.resolve('synthetic-observation-only');
afterEach(() => { vi.unstubAllEnvs(); vi.resetModules(); });
async function sink(enabled: boolean) {
    vi.resetModules();
    vi.stubEnv('SCREENPLAY_REPAIR_OBSERVE_SYNTHETIC_ROOT', enabled ? root : '');
    return import('../RepairObservation');
}
it('is a deterministic no-op by default and outside the explicitly approved synthetic scope', async () => {
    const disabled = await sink(false);
    disabled.observeRepair(root, { source: 'input:buffer-change', epoch: 1 });
    expect(disabled.readRepairObservation()).toEqual([]);
    const enabled = await sink(true);
    enabled.observeRepair(path.dirname(root), { source: 'input:buffer-change', epoch: 1 });
    expect(enabled.readRepairObservation()).toEqual([]);
});
it('bounds retained entries and exports immutable detached metadata without mutable authority', async () => {
    const enabled = await sink(true);
    for (let epoch = 0; epoch < 600; ++epoch) enabled.observeRepair(root, { source: 'invalidate:native-root:after', owner: 3, generation: 2, before: epoch, after: epoch + 1 });
    const snapshot = enabled.readRepairObservation();
    expect(snapshot).toHaveLength(512);
    expect(snapshot[0].seq).toBe(89);
    expect(Object.isFrozen(snapshot)).toBe(true);
    expect(Object.isFrozen(snapshot[0])).toBe(true);
    enabled.observeRepair(root, { source: 'guard:epoch', capturedEpoch: 2, epoch: 3 });
    expect(snapshot.at(-1)?.seq).toBe(600);
    expect(enabled.readRepairObservation().at(-1)?.seq).toBe(601);
    expect(snapshot.every(entry => Object.values(entry).every(value => ['string', 'number', 'boolean', 'undefined'].includes(typeof value)))).toBe(true);
});
it('records only bounded synthetic relative filenames and opaque hashes, never token contents or full paths', async () => {
    const enabled = await sink(true);
    expect(enabled.observationFilename(root, path.join(root, 'nested', 'watcher-existing.txt'))).toBe('nested/watcher-existing.txt');
    for (const file of [path.resolve('outside'), '../outside', 'x'.repeat(201), null]) expect(enabled.observationFilename(root, file)).toBeUndefined();
    expect(enabled.observationFilename(path.dirname(root), 'child.txt')).toBeUndefined();
    expect(enabled.observationHash('retained-secret-token')).toMatch(/^[a-f0-9]{16}$/);
    expect(enabled.observationHash('retained-secret-token')).not.toContain('retained-secret-token');
});
