// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PlayPlacement } from './PlayPlacement';

// Where the documents an import names come from - a folder on disk, or the documents of a workspace. Every
// path is portable: '/' separated and relative to one root. A path may begin with '..' when an import climbs
// above that root. The port of the C# IPlayDocumentSource.
export interface PlayDocumentSource {
    // Every .play file beneath a portable folder path, at any depth - the empty folder is the root.
    filesBeneath(folder: string): Iterable<string>;

    // The source text of a document.
    read(path: string): string;
}

// A document of an application together with where its top level belongs.
export interface PlacedPlayDocument {
    readonly path: string;
    readonly source: string;
    readonly placement: PlayPlacement;
    // False for conflicting/cyclic placement and every descendant of such an importer.
    readonly isPlacementResolved?: boolean;
}

// A document source over documents held in memory, keyed by portable path.
export function inMemoryDocumentSource(documents: ReadonlyMap<string, string>): PlayDocumentSource {
    return {
        filesBeneath: folder => [...documents.keys()].filter(path => folder.length === 0 || path.startsWith(`${folder}/`)),
        read: path => {
            const source = documents.get(path);
            if (source === undefined) {
                throw new Error(`There is no document '${path}'`);
            }
            return source;
        },
    };
}
