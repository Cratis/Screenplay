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

/** Extension-host watcher of an already approved physical root; never content authority. */
export class RepairRootWatch {
    readonly #watcher: fs.FSWatcher;
    readonly #identity: { dev: bigint; ino: bigint; physical: string };
    #disposed = false;
    #failure?: RepairFailure;
    constructor(readonly root: string, changed: () => void, readonly invalidated: (failure: RepairFailure) => void) {
        const [major, minor] = process.versions.node.split('.').map(Number);
        if (!['darwin', 'win32', 'linux'].includes(process.platform) || (process.platform === 'linux' && (major < 19 || (major === 19 && minor < 1)))) {
            throw new RepairFailure('WatchUnavailable', `Recursive root watching is unavailable in this extension host (${process.platform}, Node ${process.versions.node}). Local language assistance and read-only recovery remain available.`);
        }
        try {
            this.#identity = this.#rootIdentity();
            // One FSWatcher per connection, NOT a constant kernel-handle promise:
            // Linux may allocate per-entry watches. No probes, filenames, debounce or self-write filters.
            this.#watcher = fs.watch(root, { recursive: true }, () => {
                if (this.#disposed || this.#failure) return;
                try {
                    changed(); // EVERY notification synchronously expires review before checking identity.
                    this.check();
                } catch (error) { this.#fail(error); } // Typed, latched reporting; no silent callback failure.
            });
            this.#watcher.on('error', reason => this.#fail(reason));
            this.#watcher.on('close', () => { if (!this.#disposed) this.#fail('Root watcher closed unexpectedly.'); });
            this.check(); // Close the registration race before allowing discovery.
        } catch (error) {
            this.dispose();
            if (error instanceof RepairFailure && error.kind === 'WatchInvalidated') throw error;
            throw new RepairFailure('WatchUnavailable', 'Cannot register recursive root watching or prove physical root identity. Choose a supported native host/filesystem exposing nonzero file identity (and nonzero volume identity on Windows). No equivalent fallback or automatic retry is available. Local language assistance and read-only recovery remain available.', String(error));
        }
    }
    /** Bounded root/path checks only. Server evidence and preimages remain authoritative. */
    check(): void {
        if (this.#failure) throw this.#failure;
        if (this.#disposed) throw new RepairFailure('WatchInvalidated', 'Root watcher was disposed; reconnect before requesting authority.');
        try {
            const current = this.#rootIdentity();
            if (current.dev !== this.#identity.dev || current.ino !== this.#identity.ino || path.relative(this.#identity.physical, current.physical) !== '') throw new Error('Approved physical root was replaced.');
        } catch (error) {
            this.#fail(error);
            throw this.#failure;
        }
    }
    #rootIdentity(): { dev: bigint; ino: bigint; physical: string } {
        // Node/libuv exposes the native inode/file index and device/volume identity
        // in Stats on Unix and Windows. BigInt avoids truncating Windows file IDs.
        // Missing/zero IDs are not proof: no lexical-path or numeric fallback.
        for (let current = path.resolve(this.root); ; current = path.dirname(current)) {
            if (fs.lstatSync(current, { bigint: true }).isSymbolicLink()) throw new Error('Linked root path component.');
            if (current === path.dirname(current)) break;
        }
        const before = fs.lstatSync(this.root, { bigint: true });
        const physical = fs.realpathSync.native(this.root);
        if (path.relative(path.resolve(this.root), physical) !== '') throw new Error('Root path resolves through an unapproved link or reparse point.');
        const after = fs.statSync(physical, { bigint: true });
        if (!before.isDirectory() || before.isSymbolicLink() || !after.isDirectory() || !nativeRootIdentityAvailable(before.dev, before.ino) || !nativeRootIdentityAvailable(after.dev, after.ino) || before.dev !== after.dev || before.ino !== after.ino) throw new Error('Physical root identity cannot be proved.');
        return { dev: before.dev, ino: before.ino, physical };
    }
    #fail(reason: unknown): void {
        if (this.#failure) return;
        this.#failure = new RepairFailure('WatchInvalidated', 'Root watching lost authority. Use Screenplay: Discover Saved-File C# Repairs to deliberately reconnect and review again.', String(reason));
        try { this.invalidated(this.#failure); } finally { this.dispose(); }
    }
    dispose(): void {
        if (this.#disposed) return;
        this.#disposed = true;
        this.#watcher?.close();
    }
}
