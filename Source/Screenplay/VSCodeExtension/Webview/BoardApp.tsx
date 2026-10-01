// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import { EventModelBoard, readEventModelDocument } from '@cratis/event-models';
import { BoardProblem, BoardToExtensionMessage, ExtensionToBoardMessage } from './BoardMessage';
import { Problems } from './Problems';

interface VsCodeApi {
    postMessage(message: BoardToExtensionMessage): void;
}

declare function acquireVsCodeApi(): VsCodeApi;

const vscode = acquireVsCodeApi();

interface Board {
    readonly document: unknown;
    readonly problems: readonly BoardProblem[];
}

// The board for one .play document. The extension sends the compiled document whenever the text changes;
// the board keeps what it remembers - collapsed modules, the viewport - because the ids are stable.
export const BoardApp = () => {
    const [board, setBoard] = useState<Board | undefined>();

    useEffect(() => {
        const receive = (event: MessageEvent<ExtensionToBoardMessage>) => {
            if (event.data.type === 'show') {
                setBoard({ document: event.data.document, problems: event.data.problems });
            }
        };
        window.addEventListener('message', receive);
        vscode.postMessage({ type: 'ready' });
        return () => window.removeEventListener('message', receive);
    }, []);

    const model = useMemo(() => {
        if (board === undefined) {
            return undefined;
        }
        try {
            return readEventModelDocument(board.document);
        } catch (error) {
            return error instanceof Error ? error : new Error(String(error));
        }
    }, [board]);

    if (board === undefined || model === undefined) {
        return <div className='screenplay-board__message'>Compiling…</div>;
    }
    const showSource = (line?: number) => vscode.postMessage({ type: 'showSource', line });
    return (
        <div className='screenplay-board'>
            <Problems problems={board.problems} onShowSource={showSource} />
            <div className='screenplay-board__canvas'>
                {model instanceof Error
                    ? <div className='screenplay-board__message'>The board could not read this model: {model.message}</div>
                    : <EventModelBoard document={model} readOnly />}
            </div>
        </div>
    );
};
