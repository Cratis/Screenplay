// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// What the extension tells the board webview, and what the webview tells it back. Both sides import this,
// so a message cannot be renamed on one side only.

// One problem the compiler found, as the webview lists it.
export interface BoardProblem {
    readonly severity: 'error' | 'warning';
    readonly code: string;
    readonly message: string;
    readonly line: number;
    readonly path?: string;
}

// The board for a document: the event model document to draw and the problems found compiling it.
export interface ShowBoardMessage {
    readonly type: 'show';
    readonly document: unknown;
    readonly problems: readonly BoardProblem[];
}

export type ExtensionToBoardMessage = ShowBoardMessage;

// The webview is loaded and listening; the extension answers with the board.
export interface BoardReadyMessage {
    readonly type: 'ready';
}

// The person asked to see the source behind the board - at a line, and in a file of a folder application
// when the path relative to its root is given.
export interface ShowSourceMessage {
    readonly type: 'showSource';
    readonly line?: number;
    readonly path?: string;
}

export type BoardToExtensionMessage = BoardReadyMessage | ShowSourceMessage;
