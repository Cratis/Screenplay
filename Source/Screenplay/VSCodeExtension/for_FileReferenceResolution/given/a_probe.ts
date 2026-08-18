// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FileReferenceProbe, FileReferenceRoots, FileReferenceSearch } from '../../FileReferenceResolution';

// A file system of exactly the files it is told about, so a spec states the tree it means rather than
// standing one up on disk. It also records what it was asked, which is how the specs pin the order the
// ladder probes in — the order is the behavior, not an implementation detail.
//
// The search honors a cap, because the real one does. A double that always answers exhaustively cannot
// tell a complete answer from a cut-short one, which is the whole of what the truncation rule is about.
export class a_probe implements FileReferenceProbe {
    readonly probed: string[] = [];
    readonly searched: string[] = [];

    constructor(
        private readonly files: readonly string[],
        private readonly searchLimit = Number.POSITIVE_INFINITY,
    ) {}

    async exists(candidate: string): Promise<boolean> {
        this.probed.push(candidate);
        return this.files.includes(candidate);
    }

    async search(baseName: string): Promise<FileReferenceSearch> {
        this.searched.push(baseName);
        const found = this.files.filter((file) => file.split('/').pop() === baseName);
        return {
            files: found.slice(0, this.searchLimit),
            truncated: found.length > this.searchLimit,
        };
    }
}

// The layout every spec resolves against unless it says otherwise.
export function the_roots(overrides: Partial<FileReferenceRoots> = {}): FileReferenceRoots {
    return {
        configured: [],
        workspaceFolder: '/repo',
        documentDirectory: '/repo/Documentation',
        sourceRoots: ['/repo/Source', '/repo/Source/Invoicing'],
        ...overrides,
    };
}
