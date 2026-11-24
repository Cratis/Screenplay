// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createHash } from 'node:crypto';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { RepairFailure } from './RepairClient';

/** Private byte authority from the server's complete source preimages, never from editor text or live disk. */
export class RepairSourceSnapshot {
    readonly #sources: { path: string; length: number; hash: string }[] = [];
    readonly #destinations: { path: string; original: string | null }[] = [];

    constructor(entries: readonly { beforePath: unknown; afterPath: unknown; before: Buffer | null; after: Buffer | null }[]) {
        for (const entry of entries) {
            const before = this.#sourcePath(entry.beforePath, entry.before);
            const after = this.#sourcePath(entry.afterPath, entry.after);
            if (before === null && after === null) this.#malformed();
            if (before !== null) this.#sources.push({ path: before, length: entry.before!.length, hash: this.#hash(entry.before!) });
            if (after !== null) this.#destinations.push({ path: after, original: before });
        }
        this.#requireUnique(this.#sources);
        this.#requireUnique(this.#destinations);
    }

    /** Synchronous actual-dispatch guard. Watcher delivery is not required and no filesystem metadata is cached. */
    check(root: string): void {
        for (const source of this.#sources) this.#verify(source.path, () => this.#checkSource(root, source));
        const originals = new Set(this.#sources.map(source => source.path));
        for (const destination of this.#destinations.filter(destination => !originals.has(destination.path))) {
            this.#verify(destination.path, () => this.#checkDestination(root, destination));
        }
    }

    #sourcePath(value: unknown, bytes: Buffer | null): string | null {
        if (value === null && bytes === null) return null;
        if (typeof value !== 'string' || bytes === null) return this.#malformed();
        if (!this.#safePath(value)) return this.#malformed();
        return value;
    }

    #safePath(value: string): boolean {
        if (path.posix.isAbsolute(value)) return false;
        if (/[\\:\0]/.test(value)) return false;
        return value.split('/').every(part => !['', '.', '..'].includes(part));
    }

    #requireUnique(entries: readonly { path: string }[]): void {
        if (new Set(entries.map(entry => entry.path)).size !== entries.length) this.#malformed();
    }

    #malformed(): never {
        throw new RepairFailure('MalformedContract', 'Source preimage paths and bytes are inconsistent or unsafe.');
    }

    #hash(bytes: Buffer): string { return createHash('sha256').update(bytes).digest('hex'); }

    #verify(relative: string, check: () => void): void {
        try { check(); }
        catch {
            throw new RepairFailure('StaleSource', `Repair preview is stale: "${relative}" changed or is no longer available. Nothing was applied. Run Screenplay: Discover Saved-File C# Repairs to refresh and review again.`);
        }
    }

    #stat(root: string, relative: string): fs.Stats | undefined {
        const parts = relative.split('/');
        let current = root;
        for (const part of parts.slice(0, -1)) {
            current = path.join(current, part);
            const parent = this.#unlinkedStat(current);
            if (!parent) return undefined;
            if (!parent.isDirectory()) throw new Error('Non-directory source ancestor.');
        }
        return this.#unlinkedStat(path.join(current, parts[parts.length - 1]));
    }

    #unlinkedStat(file: string): fs.Stats | undefined {
        let stat: fs.Stats;
        try { stat = fs.lstatSync(file); }
        catch (error) { if ((error as NodeJS.ErrnoException).code === 'ENOENT') return undefined; throw error; }
        if (stat.isSymbolicLink()) throw new Error('Linked source path.');
        return stat;
    }

    #regularPreimage(stat: fs.Stats | undefined, length: number): void {
        if (!stat?.isFile()) throw new Error('Source kind changed.');
        if (stat.size !== length) throw new Error('Source size changed.');
    }

    #open(file: string): number {
        // Nonblocking/no-follow where supported; even a replacement must be a bounded regular file.
        return fs.openSync(file, fs.constants.O_RDONLY | (fs.constants.O_NOFOLLOW ?? 0) | (fs.constants.O_NONBLOCK ?? 0));
    }

    #checkSource(root: string, source: { path: string; length: number; hash: string }): void {
        this.#regularPreimage(this.#stat(root, source.path), source.length);
        const descriptor = this.#open(path.join(root, source.path));
        try { this.#checkBytes(descriptor, source); }
        finally { fs.closeSync(descriptor); }
    }

    #checkBytes(descriptor: number, source: { length: number; hash: string }): void {
        this.#regularPreimage(fs.fstatSync(descriptor), source.length);
        const bytes = Buffer.alloc(source.length);
        const count = fs.readSync(descriptor, bytes, 0, bytes.length, 0);
        if (count !== source.length) throw new Error('Source changed during read.');
        this.#regularPreimage(fs.fstatSync(descriptor), source.length);
        if (this.#hash(bytes) !== source.hash) throw new Error('Source bytes changed.');
    }

    #checkDestination(root: string, destination: { path: string; original: string | null }): void {
        const target = this.#stat(root, destination.path);
        if (!target) return;
        const original = destination.original === null ? undefined : this.#stat(root, destination.original);
        // Case-only renames on case-insensitive filesystems resolve to the original itself.
        if (this.#samePhysicalFile(original, target)) return;
        throw new Error('Repair destination is no longer absent.');
    }

    #samePhysicalFile(original: fs.Stats | undefined, target: fs.Stats): boolean {
        if (!original?.isFile()) return false;
        if (target.ino === 0) return false;
        return target.dev === original.dev && target.ino === original.ino;
    }
}
