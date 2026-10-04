// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'node:fs';
import { RepairFailure } from './RepairClient';

/** Extension-host watcher of an already approved physical root; never path authority. */
export class RepairRootWatch {
    readonly #watcher: fs.FSWatcher;
    #disposed = false;
    #failed = false;
    constructor(root: string, changed: () => void, invalidated: (failure: RepairFailure) => void) {
        const [major, minor] = process.versions.node.split('.').map(Number);
        if (!['darwin', 'win32', 'linux'].includes(process.platform) || (process.platform === 'linux' && (major < 19 || (major === 19 && minor < 1)))) {
            throw new RepairFailure('WatchUnavailable', `Recursive root watching is unavailable in this extension host (${process.platform}, Node ${process.versions.node}). Local language assistance and read-only recovery remain available.`);
        }
        const fail = (reason: unknown) => {
            if (this.#disposed || this.#failed) return;
            this.#failed = true;
            invalidated(new RepairFailure('WatchInvalidated', 'Root watching lost authority. Use Screenplay: Discover Saved-File C# Repairs to deliberately reconnect and review again.', String(reason)));
            this.dispose();
        };
        try {
            // One FSWatcher per connection, NOT a constant kernel-handle promise:
            // Linux may allocate per-entry watches. No probes, filenames, debounce or self-write filters.
            this.#watcher = fs.watch(root, { recursive: true }, event => {
                if (this.#disposed || this.#failed) return;
                if (event === 'change') changed();
                else fail(`Root watch notification: ${event}`);
            });
            this.#watcher.on('error', fail);
            this.#watcher.on('close', () => fail('Root watcher closed unexpectedly.'));
        } catch (error) {
            throw new RepairFailure('WatchUnavailable', 'Cannot register recursive root watching. No equivalent fallback or automatic retry is available. Local language assistance and read-only recovery remain available.', String(error));
        }
    }
    dispose(): void {
        if (this.#disposed) return;
        this.#disposed = true;
        this.#watcher?.close();
    }
}
