// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { PlayFileSource } from '@cratis/screenplay-compiler';
import type { VisualizedDocument } from './VisualizedDocument';

// The documents as a change would leave them: a changed document replaces the one at its path, a new one
// is added, and one without source is taken away.
export function applyChanges(documents: readonly VisualizedDocument[], changes: readonly VisualizedDocument[]): PlayFileSource[] {
    const result = new Map<string, string>();
    for (const document of documents) {
        if (document.source !== undefined) {
            result.set(document.path, document.source);
        }
    }
    for (const change of changes) {
        if (change.source === undefined) {
            result.delete(change.path);
        } else {
            result.set(change.path, change.source);
        }
    }
    return [...result].map(([path, source]) => ({ path, source }));
}
