// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BoardToExtensionMessage } from './BoardMessage';

// The part of the API VS Code gives a webview that the board uses: messages to the extension, and state
// VS Code keeps for the webview while its editor is open - also across the webview being hidden.
interface VsCodeApi {
    postMessage(message: BoardToExtensionMessage): void;
    getState(): unknown;
    setState(state: unknown): void;
}

declare function acquireVsCodeApi(): VsCodeApi;

// A webview may acquire the API once, so every module shares this one.
export const vscode = acquireVsCodeApi();
