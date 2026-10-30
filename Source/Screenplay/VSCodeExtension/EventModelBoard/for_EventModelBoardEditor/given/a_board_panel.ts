// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { WebviewPanel } from 'vscode';
import type { BoardToExtensionMessage, ExtensionToBoardMessage } from '../../../Webview/BoardMessage';

export function a_board_panel() {
    const messages: ExtensionToBoardMessage[] = [];
    let receive: ((message: BoardToExtensionMessage) => unknown) | undefined;
    let dispose: (() => void) | undefined;
    const panel = {
        webview: {
            options: {}, html: '', cspSource: 'test',
            asWebviewUri: (uri: unknown) => uri,
            postMessage: async (message: ExtensionToBoardMessage) => { messages.push(message); return true; },
            onDidReceiveMessage: (listener: typeof receive) => {
                receive = listener;
                return { dispose: () => { receive = undefined; } };
            },
        },
        onDidDispose: (listener: () => void) => { dispose = listener; },
    } as unknown as WebviewPanel;
    return { panel, messages, receive: async (message: BoardToExtensionMessage) => { await receive?.(message); }, dispose: () => dispose?.() };
}
