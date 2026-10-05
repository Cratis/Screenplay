// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, afterEach, it, expect, vi } from 'vitest';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { EventEmitter } from 'node:events';
vi.mock('node:fs', async importOriginal => ({ ...await importOriginal<typeof fs>(), watch: vi.fn() }));
import { RepairRootWatch, nativeRootIdentityAvailable } from '../RepairRootWatch';

let native: EventEmitter & { close: ReturnType<typeof vi.fn> };
let callback: (event: string, filename?: string | null) => void;
let changed = vi.fn<() => void>(), invalidated = vi.fn<(failure: unknown, cause: string) => void>();
let root: string;
beforeEach(() => {
    const tasks = path.resolve('../../../.ai-work'); fs.mkdirSync(tasks, { recursive: true });
    root = fs.realpathSync.native(fs.mkdtempSync(path.join(tasks, 'watch-root-')));
    native = Object.assign(new EventEmitter(), { close: vi.fn() });
    changed = vi.fn(); invalidated = vi.fn();
    vi.mocked(fs.watch).mockReset().mockImplementation(((_root: string, options: fs.WatchOptions, listener: typeof callback) => {
        expect(options).toEqual({ recursive: true });
        callback = listener;
        return native;
    }) as unknown as typeof fs.watch);
});
afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); });
it('expires every notification synchronously without names, deduplication or self-write suppression, retaining a healthy root', () => {
    const watch = new RepairRootWatch(root, changed, invalidated);
    callback('change', 'nested/Handler.cs'); callback('rename', null); callback('rename'); callback('change', 'nested/Handler.cs');
    expect(changed).toHaveBeenCalledTimes(4);
    expect(invalidated).not.toHaveBeenCalled();
    watch.check();
    watch.dispose(); watch.dispose(); callback('change'); native.emit('close');
    expect(native.close).toHaveBeenCalledTimes(1);
    expect(changed).toHaveBeenCalledTimes(4);
    expect(invalidated).not.toHaveBeenCalled();
});
for (const mode of ['error', 'overflow', 'close']) it(`latches reconnect required for ${mode}; no retry or late authority`, () => {
    const watch = new RepairRootWatch(root, changed, invalidated);
    native.emit(mode === 'overflow' ? 'error' : mode, new Error(mode));
    callback('change'); native.emit('close'); watch.dispose();
    expect(invalidated).toHaveBeenCalledTimes(1);
    expect(invalidated.mock.calls[0][0]).toMatchObject({ kind: 'WatchInvalidated' });
    expect(changed).not.toHaveBeenCalled();
    expect(native.close).toHaveBeenCalledTimes(1);
    expect(() => watch.check()).toThrow(expect.objectContaining({ kind: 'WatchInvalidated' }));
    expect(fs.watch).toHaveBeenCalledTimes(1);
});
for (const mode of ['replacement', 'missing', 'link']) it(`expires the event BEFORE refusing actual root ${mode}`, () => {
    const watch = new RepairRootWatch(root, changed, invalidated);
    fs.renameSync(root, root + '-original');
    if (mode === 'replacement') fs.mkdirSync(root);
    if (mode === 'link') fs.symlinkSync(root + '-original', root, 'junction');
    callback('rename', 'child.cs');
    expect(changed).toHaveBeenCalledTimes(1);
    expect(changed.mock.invocationCallOrder[0]).toBeLessThan(invalidated.mock.invocationCallOrder[0]);
    expect(invalidated).toHaveBeenCalledWith(expect.objectContaining({ kind: 'WatchInvalidated' }), 'root-identity');
    expect(native.close).toHaveBeenCalledTimes(1);
    callback('change'); expect(changed).toHaveBeenCalledTimes(1);
    watch.dispose();
});
it('checks root replacement before new authority even without a delivered notification', () => {
    const watch = new RepairRootWatch(root, changed, invalidated);
    fs.renameSync(root, root + '-original'); fs.mkdirSync(root);
    expect(() => watch.check()).toThrow(expect.objectContaining({ kind: 'WatchInvalidated' }));
    expect(invalidated).toHaveBeenCalledTimes(1);
});
it('fails closed when the host cannot expose a usable native file identity', () => {
    const stat = fs.lstatSync(root, { bigint: true });
    vi.spyOn(fs, 'lstatSync').mockReturnValue(Object.assign(stat, { ino: 0n }));
    expect(() => new RepairRootWatch(root, changed, invalidated)).toThrow(expect.objectContaining({ kind: 'WatchUnavailable' }));
    expect(fs.watch).not.toHaveBeenCalled();
});
for (const platform of ['win32', 'linux', 'darwin'] as const) it(`checks native identity sentinels on ${platform} without loss of precision`, () => {
    expect(nativeRootIdentityAvailable(0n, 42n, platform)).toBe(platform !== 'win32');
    expect(nativeRootIdentityAvailable(0n, 0n, platform)).toBe(false);
    expect(nativeRootIdentityAvailable(7n, 0n, platform)).toBe(false);
    expect(nativeRootIdentityAvailable(-1n, 42n, platform)).toBe(false);
    expect(nativeRootIdentityAvailable(7, 42n, platform)).toBe(false);
    expect(nativeRootIdentityAvailable(7n, 42, platform)).toBe(false);
    expect(nativeRootIdentityAvailable(7n, 9007199254740993n, platform)).toBe(true);
});
it('refuses ambiguous Windows zero-volume identity before registering, and latches loss after registration', () => {
    const lstat = fs.lstatSync.bind(fs), stat = fs.statSync.bind(fs);
    const normalize = (value: fs.BigIntStats) => Object.assign(value, { dev: 0n });
    vi.stubGlobal('process', { ...process, platform: 'win32' });
    vi.spyOn(fs, 'lstatSync').mockImplementation(((...args: Parameters<typeof lstat>) => normalize(lstat(...args) as unknown as fs.BigIntStats)) as typeof fs.lstatSync);
    vi.spyOn(fs, 'statSync').mockImplementation(((...args: Parameters<typeof stat>) => normalize(stat(...args) as unknown as fs.BigIntStats)) as typeof fs.statSync);
    expect(() => new RepairRootWatch(root, changed, invalidated)).toThrow(expect.objectContaining({ kind: 'WatchUnavailable' }));
    expect(fs.watch).not.toHaveBeenCalled();
    vi.restoreAllMocks(); vi.unstubAllGlobals();
    const watch = new RepairRootWatch(root, changed, invalidated);
    vi.spyOn(fs, 'lstatSync').mockReturnValue(Object.assign(lstat(root, { bigint: true }), { ino: 0n }));
    callback('change', 'child');
    expect(changed).toHaveBeenCalledTimes(1);
    expect(() => watch.check()).toThrow(expect.objectContaining({ kind: 'WatchInvalidated' }));
    expect(invalidated).toHaveBeenCalledTimes(1);
});
it('accepts platform-native root casing but rejects a different physical path', () => {
    const realpath = fs.realpathSync.native;
    const caseChanged = root.toUpperCase();
    vi.spyOn(fs.realpathSync, 'native').mockReturnValue(caseChanged);
    if (process.platform === 'win32') {
        const watch = new RepairRootWatch(root, changed, invalidated); watch.check(); watch.dispose();
    } else if (caseChanged !== realpath(root)) {
        expect(() => new RepairRootWatch(root, changed, invalidated)).toThrow(expect.objectContaining({ kind: 'WatchUnavailable' }));
    }
});
for (const code of ['ERR_FEATURE_UNAVAILABLE_ON_PLATFORM', 'ENOSPC', 'EMFILE']) it(`refuses ${code} registration with typed unavailability and no fallback`, () => {
    vi.mocked(fs.watch).mockImplementation(() => { throw Object.assign(new Error(code), { code }); });
    expect(() => new RepairRootWatch(root, changed, invalidated)).toThrow(expect.objectContaining({ kind: 'WatchUnavailable' }));
    expect(fs.watch).toHaveBeenCalledTimes(1);
});
