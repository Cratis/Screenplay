// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import { EventModelBoard, EventModelPresentationProvider, MenuDropdownOpenProvider, readEventModelDocument } from '@cratis/event-models';
import { ToolbarButton } from '@cratis/components/Toolbar';
import { DependencyMapView, DependencyView } from '@cratis/screenplay-views';
import type { DependencyMap } from '@cratis/screenplay-event-models';
import '@cratis/screenplay-views/dependency-map.css';
import { boardChrome } from './boardChrome';
import { BoardErrorBoundary } from './BoardErrorBoundary';
import { BoardProblem, ExtensionToBoardMessage } from './BoardMessage';
import { Problems } from './Problems';
import { defaultPresentation, usePresentation } from './usePresentation';
import { vscode } from './vscodeApi';

interface Board {
    readonly document: unknown;
    readonly dependencies: DependencyMap;
    readonly problems: readonly BoardProblem[];
}

// The board for one .play document. The extension sends the compiled document whenever the text changes;
// the board keeps what it remembers - collapsed modules, the viewport - because the ids are stable.
export const BoardApp = () => {
    const [board, setBoard] = useState<Board | undefined>();
    const [view, setView] = useState(DependencyView.Board);
    const [presentation, changePresentation] = usePresentation();

    useEffect(() => {
        const receive = (event: MessageEvent<ExtensionToBoardMessage>) => {
            if (event.data.type === 'show') {
                setBoard({ document: event.data.document, dependencies: event.data.dependencies, problems: event.data.problems });
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
    const showSource = (line?: number, path?: string) => vscode.postMessage({ type: 'showSource', line, path });
    const tools = <>
        <ToolbarButton text='Board' title='Event model board' active={view === DependencyView.Board} tooltipPosition='bottom' onClick={() => setView(DependencyView.Board)} />
        <ToolbarButton text='Map' title='Module and feature dependencies' active={view === DependencyView.Map} tooltipPosition='bottom' onClick={() => setView(DependencyView.Map)} />
    </>;
    return (
        <div className='screenplay-board'>
            <Problems problems={board.problems} onShowSource={showSource} />
            <div className='screenplay-board__canvas'>
                {view === DependencyView.Map ? <div className='screenplay-dependency-map-host'>
                    <div className='screenplay-dependency-map-toolbar' role='toolbar' aria-label='Board view'>{tools}</div>
                    <DependencyMapView map={board.dependencies} onShowSource={showSource} />
                </div> : model instanceof Error
                    ? <div className='screenplay-board__message'>The board could not read this model: {model.message}</div>
                    : (
                        <MenuDropdownOpenProvider>
                            <EventModelPresentationProvider presentation={presentation} onChange={changePresentation}>
                                <BoardErrorBoundary resetWhenChanged={presentation} onReset={() => changePresentation(defaultPresentation)}>
                                    <EventModelBoard document={model} readOnly showLogo showViewOptions toolbar={tools} canvas={{ chrome: boardChrome }} />
                                </BoardErrorBoundary>
                            </EventModelPresentationProvider>
                        </MenuDropdownOpenProvider>
                    )}
            </div>
        </div>
    );
};
