// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'node:fs';
import * as path from 'node:path';
import { RepairFailure } from './RepairClient';

/** Whether native stat supplies usable identity, without numeric/path fallbacks. */
export function nativeRootIdentityAvailable(dev: unknown, ino: unknown, platform: NodeJS.Platform = process.platform): boolean {
    // libuv v1.52.1 win/fs.c maps STATUS_NOT_IMPLEMENTED volume information
    // to st_dev=0. A genuine zero volume serial is indistinguishable: refuse it.
    // unix/fs.c copies native st_dev unchanged; zero is NOT that sentinel on Unix.
    return typeof dev === 'bigint' && typeof ino === 'bigint' && dev >= 0n && ino > 0n && (platform !== 'win32' || dev !== 0n);
}

/** Immutable physical identity of an approved root. */
export interface RootIdentity { readonly dev: bigint; readonly ino: bigint; readonly physical: string }

/** Proves the root's physical identity now. Missing/zero IDs are not proof: no lexical-path or numeric fallback. */
export function captureRootIdentity(root: string): RootIdentity {
    // Node/libuv exposes the native inode/file index and device/volume identity
    // in Stats on Unix and Windows. BigInt avoids truncating Windows file IDs.
    for (let current = path.resolve(root); ; current = path.dirname(current)) {
        if (fs.lstatSync(current, { bigint: true }).isSymbolicLink()) throw new Error('Linked root path component.');
        if (current === path.dirname(current)) break;
    }
    const before = fs.lstatSync(root, { bigint: true });
    const physical = fs.realpathSync.native(root);
    if (path.relative(path.resolve(root), physical) !== '') throw new Error('Root path resolves through an unapproved link or reparse point.');
    const after = fs.statSync(physical, { bigint: true });
    if (!before.isDirectory() || before.isSymbolicLink() || !after.isDirectory() || !nativeRootIdentityAvailable(before.dev, before.ino) || !nativeRootIdentityAvailable(after.dev, after.ino) || before.dev !== after.dev || before.ino !== after.ino) throw new Error('Physical root identity cannot be proved.');
    return Object.freeze({ dev: before.dev, ino: before.ino, physical });
}

export function sameRootIdentity(left: RootIdentity, right: RootIdentity): boolean {
    return left.dev === right.dev && left.ino === right.ino && path.relative(left.physical, right.physical) === '';
}

/** Validates a retained identity independently of any watcher lifetime. Never clears uncertainty. */
export function assertRootIdentity(root: string, identity: RootIdentity): void {
    let current: RootIdentity;
    try { current = captureRootIdentity(root); }
    catch (error) { throw new RepairFailure('RootRefused', 'The uncertain Apply root identity cannot be proved.', String(error)); }
    if (!sameRootIdentity(current, identity)) throw new RepairFailure('RootRefused', 'The uncertain Apply root was replaced; inspection of the replacement is refused.');
}

/** Extension-host watcher of an already approved physical root; never content authority. */
export class RepairRootWatch {
    readonly #watcher: fs.FSWatcher;
    readonly #identity: RootIdentity;
    #disposed = false;
    #failure?: RepairFailure;
    constructor(readonly root: string, changed: (event: string, filename: string | null) => void, readonly invalidated: (failure: RepairFailure, cause: string) => void) {
        const [major, minor] = process.versions.node.split('.').map(Number);
        if (!['darwin', 'win32', 'linux'].includes(process.platform) || (process.platform === 'linux' && (major < 19 || (major === 19 && minor < 1)))) {
            throw new RepairFailure('WatchUnavailable', `Recursive root watching is unavailable in this extension host (${process.platform}, Node ${process.versions.node}). Local language assistance and read-only recovery remain available.`);
        }
        try {
            this.#identity = captureRootIdentity(this.root);
            // One FSWatcher per connection, NOT a constant kernel-handle promise:
            // Linux may allocate per-entry watches. No probes, filenames, debounce or self-write filters.
            this.#watcher = fs.watch(root, { recursive: true }, (event, filename) => {
                if (this.#disposed || this.#failure) return;
                try {
                    changed(event, filename?.toString() ?? null); // EVERY notification synchronously expires review before checking identity.
                    this.check();
                } catch (error) { this.#fail(error, 'native-callback'); } // Typed, latched reporting; no silent callback failure.
            });
            this.#watcher.on('error', reason => this.#fail(reason, 'native-error'));
            this.#watcher.on('close', () => { if (!this.#disposed) this.#fail('Root watcher closed unexpectedly.', 'native-close'); });
            this.check(); // Close the registration race before allowing discovery.
        } catch (error) {
            this.dispose();
            if (error instanceof RepairFailure && error.kind === 'WatchInvalidated') throw error;
            throw new RepairFailure('WatchUnavailable', 'Cannot register recursive root watching or prove physical root identity. Choose a supported native host/filesystem exposing nonzero file identity (and nonzero volume identity on Windows). No equivalent fallback or automatic retry is available. Local language assistance and read-only recovery remain available.', String(error));
        }
    }
    /** Immutable physical identity captured at registration; usable after this watcher is disposed. */
    get identity(): RootIdentity { return this.#identity; }
    /** Bounded root/path checks only. Server evidence and preimages remain authoritative. */
    check(): void {
        if (this.#failure) throw this.#failure;
        if (this.#disposed) throw new RepairFailure('WatchInvalidated', 'Root watcher was disposed; reconnect before requesting authority.');
        try {
            if (!sameRootIdentity(captureRootIdentity(this.root), this.#identity)) throw new Error('Approved physical root was replaced.');
        } catch (error) {
            this.#fail(error, 'root-identity');
            throw this.#failure;
        }
    }
    #fail(reason: unknown, cause: string): void {
        if (this.#failure) return;
        this.#failure = new RepairFailure('WatchInvalidated', 'Root watching lost authority. Use Screenplay: Discover Saved-File C# Repairs to deliberately reconnect and review again.', String(reason));
        try { this.invalidated(this.#failure, cause); } finally { this.dispose(); }
    }
    dispose(): void {
        if (this.#disposed) return;
        this.#disposed = true;
        this.#watcher?.close();
    }
}
