// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';

// The file a board may open for a path its webview asked for. The webview is untrusted, so the path is never
// resolved by arithmetic: it must be exactly one of the .play files the board compiled, named relative to the
// root the board compiled them from. Documented imports that climb out of the folder are in that set verbatim, so
// they keep working, while anything the compile did not list - parent traversal, absolute and drive paths, other
// separators, encodings - is refused. A symlinked .play file the workspace itself lists is part of the board.
export function resolveBoardSource(root: vscode.Uri | undefined, compiled: ReadonlySet<string>, requested: unknown): vscode.Uri | undefined {
    if (root === undefined || typeof requested !== 'string' || !requested.endsWith('.play') || !compiled.has(requested)) {
        return undefined;
    }
    return vscode.Uri.joinPath(root, requested);
}
