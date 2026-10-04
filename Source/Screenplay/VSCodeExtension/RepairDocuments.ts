// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'node:fs';
import * as path from 'node:path';
import type { Uri } from 'vscode';

export function contains(root: string, file: string): boolean {
    const relative = path.relative(root, file);
    return relative === '' || (!relative.startsWith(`..${path.sep}`) && relative !== '..' && !path.isAbsolute(relative));
}

// Resolve existing ancestors too: associated untitled files need not exist yet.
function physicalPath(file: string): string {
    let ancestor = path.resolve(file);
    const suffix: string[] = [];
    while (!fs.existsSync(ancestor)) {
        const parent = path.dirname(ancestor);
        if (parent === ancestor) throw new Error('Cannot resolve document filesystem ancestry.');
        suffix.unshift(path.basename(ancestor));
        ancestor = parent;
    }
    return path.join(fs.realpathSync.native(ancestor), ...suffix);
}

export type RootDocument = { scope: 'root' | 'outside'; physical: string } | { scope: 'ambiguous' | 'virtual' };

/** One policy for source, attachments and identity state, before and after dispatch. */
export function classifyRootDocument(root: string, uri: Uri): RootDocument {
    if (uri.scheme !== 'file' && uri.scheme !== 'untitled') return { scope: 'virtual' };
    // Untitled-1 has no associated filesystem path. Its eventual destination is
    // unknown: conservatively block repair, but do not block proven outside paths.
    // Restoring the file scheme also restores native UNC fsPath interpretation
    // for associated untitled URIs on Windows. Never infer a path from a label.
    const file = uri.scheme === 'untitled' && typeof uri.with === 'function' ? uri.with({ scheme: 'file' }).fsPath : uri.fsPath;
    if (!path.isAbsolute(file) || (uri.scheme === 'untitled' && (uri.query || uri.fragment))) return { scope: 'ambiguous' };
    try {
        const physical = physicalPath(file);
        return { scope: contains(path.resolve(root), path.resolve(file)) || contains(physicalPath(root), physical) ? 'root' : 'outside', physical };
    } catch { return { scope: 'ambiguous' }; }
}
