// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// What the extension tells the board webview, and what the webview tells it back. Both sides import this,
// so a message cannot be renamed on one side only.

// One problem the compiler found, as the webview lists it.
export interface BoardProblem {
    readonly severity: 'error' | 'warning' | 'information';
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

// How the person last chose to view boards: how much of each slice is drawn, whether properties are
// shown and how connections are drawn. Kept by the extension for the person, across boards and sessions.
export interface BoardViewOptions {
    readonly detailLevel: 'full' | 'overview';
    readonly showProperties: boolean;
    readonly visualizationMode: 'simplified' | 'fillLines';
}

// The view options to show the board with - sent when the board is ready, and when another board changes them.
export interface ViewOptionsMessage {
    readonly type: 'viewOptions';
    readonly options: BoardViewOptions;
}

export type ExtensionToBoardMessage = ShowBoardMessage | ViewOptionsMessage;

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

// The person changed how the board is viewed; the extension keeps it for every board.
export interface ViewOptionsChangedMessage {
    readonly type: 'viewOptionsChanged';
    readonly options: BoardViewOptions;
}

export type BoardToExtensionMessage = BoardReadyMessage | ShowSourceMessage | ViewOptionsChangedMessage;
