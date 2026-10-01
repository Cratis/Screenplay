// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';

// The file a folder of .play documents keeps at its root - what the C# PlayFileWriter writes when it
// expands an application into folders, and so what marks a folder as one application.
export const applicationFileName = 'application.play';

// How far up from a document the search goes when the document is outside any workspace folder.
const searchDepth = 16;

// The folder of the application a document belongs to: the nearest folder at or above the document's own
// that holds an application.play, without leaving the workspace folder. A document outside any such folder
// is an application of its own, and undefined says so.
export async function findApplicationRoot(
    documentPath: string,
    workspaceFolder: string | undefined,
    hasFile: (filePath: string) => Promise<boolean>): Promise<string | undefined> {
    let folder = path.dirname(documentPath);
    for (let depth = 0; depth < searchDepth; depth++) {
        if (await hasFile(path.join(folder, applicationFileName))) {
            return folder;
        }
        const parent = path.dirname(folder);
        if (parent === folder || (workspaceFolder !== undefined && !isWithin(parent, workspaceFolder))) {
            return undefined;
        }
        folder = parent;
    }
    return undefined;
}

function isWithin(folder: string, container: string): boolean {
    const relative = path.relative(container, folder);
    return relative === '' || (!relative.startsWith('..') && !path.isAbsolute(relative));
}
