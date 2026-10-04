// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { VisualizedModel } from './VisualizedModel';

// What identifies the application as it is on disk: its name and every document, in path order. Two
// results with the same signature draw the same board, so a refresh that finds the same signature
// changes nothing on screen.
export function documentsSignature(model: Pick<VisualizedModel, 'application' | 'documents'>): string {
    const documents = [...model.documents]
        .sort((left, right) => (left.path < right.path ? -1 : left.path > right.path ? 1 : 0))
        .map(document => [document.path, document.source ?? '']);
    return JSON.stringify([model.application, documents]);
}
