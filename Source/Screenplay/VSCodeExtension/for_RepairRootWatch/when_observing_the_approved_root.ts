// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, afterEach, it, expect, vi } from 'vitest';
import * as fs from 'node:fs';
import { EventEmitter } from 'node:events';
vi.mock('node:fs', async importOriginal => ({ ...await importOriginal<typeof fs>(), watch: vi.fn() }));
import { RepairRootWatch } from '../RepairRootWatch';

let native: EventEmitter & { close: ReturnType<typeof vi.fn> };
let callback: (event: string, filename?: string | null) => void;
let changed = vi.fn<() => void>(), invalidated = vi.fn<(failure: unknown) => void>();
beforeEach(() => {
    native = Object.assign(new EventEmitter(), { close: vi.fn() });
    changed = vi.fn(); invalidated = vi.fn();
    vi.mocked(fs.watch).mockReset().mockImplementation(((_root: string, options: fs.WatchOptions, listener: typeof callback) => {
        expect(options).toEqual({ recursive: true });
        callback = listener;
        return native;
    }) as unknown as typeof fs.watch);
});
afterEach(() => vi.restoreAllMocks());
it('handles every change synchronously without filename guessing, deduplication or self-write suppression', () => {
    const watch = new RepairRootWatch('/approved', changed, invalidated);
    callback('change', 'nested/Handler.cs'); callback('change', null); callback('change'); callback('change', 'nested/Handler.cs');
    expect(changed).toHaveBeenCalledTimes(4);
    expect(invalidated).not.toHaveBeenCalled();
    watch.dispose(); watch.dispose(); callback('change'); native.emit('close');
    expect(native.close).toHaveBeenCalledTimes(1);
    expect(changed).toHaveBeenCalledTimes(4);
    expect(invalidated).not.toHaveBeenCalled();
});
for (const mode of ['rename', 'error', 'overflow', 'close']) it(`latches reconnect required for ${mode}; no retry or late authority`, () => {
    const watch = new RepairRootWatch('/approved', changed, invalidated);
    if (mode === 'rename') callback('rename');
    else native.emit(mode === 'overflow' ? 'error' : mode, new Error(mode));
    callback('change'); native.emit('close'); watch.dispose();
    expect(invalidated).toHaveBeenCalledTimes(1);
    expect(invalidated.mock.calls[0][0]).toMatchObject({ kind: 'WatchInvalidated' });
    expect(changed).not.toHaveBeenCalled();
    expect(native.close).toHaveBeenCalledTimes(1);
    expect(fs.watch).toHaveBeenCalledTimes(1);
});
for (const code of ['ERR_FEATURE_UNAVAILABLE_ON_PLATFORM', 'ENOSPC', 'EMFILE']) it(`refuses ${code} registration with typed unavailability and no fallback`, () => {
    vi.mocked(fs.watch).mockImplementation(() => { throw Object.assign(new Error(code), { code }); });
    expect(() => new RepairRootWatch('/approved', changed, invalidated)).toThrow(expect.objectContaining({ kind: 'WatchUnavailable' }));
    expect(fs.watch).toHaveBeenCalledTimes(1);
});
