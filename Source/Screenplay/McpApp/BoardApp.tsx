// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import { EventModelBoard, MenuDropdownOpenProvider, readEventModelDocument } from '@cratis/event-models';
import { DependencyMapView, DependencyView } from '@cratis/screenplay-views';
import '@cratis/screenplay-views/dependency-map.css';
import { boardChrome } from './boardChrome';
import { BoardErrorBoundary } from './BoardErrorBoundary';
import { boardHeight } from './boardHeight';
import { BoardTools } from './BoardTools';
import { compileBoards } from './compileBoards';
import { ShownModel } from './ShownModel';
import { useHost } from './useHost';

// The event model board for what the visualize-model tool sent. With a proposal or a sketch the board
// starts on what the change would make of the application, and the toolbar switches to how it is now.
export const BoardApp = () => {
    const host = useHost();
    const [shown, setShown] = useState(ShownModel.Proposed);
    const [view, setView] = useState(DependencyView.Board);
    const [attempt, setAttempt] = useState(0);
    const boards = useMemo(() => {
        if (host.model === undefined || host.model instanceof Error) {
            return host.model;
        }
        try {
            return compileBoards(host.model);
        } catch (error) {
            return error instanceof Error ? error : new Error(String(error));
        }
    }, [host.model]);
    const compiled = boards === undefined || boards instanceof Error ? undefined : boards;
    const board = compiled && (shown === ShownModel.Proposed && compiled.proposed ? compiled.proposed : compiled.current);
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

    const style = { height: boardHeight(host.container) };
    if (boards instanceof Error || model instanceof Error) {
        return <div className='screenplay-mcp-board__message' style={style}>The board could not be drawn: {(boards instanceof Error ? boards : model as Error).message}</div>;
    }
    if (board === undefined || model === undefined) {
        return <div className='screenplay-mcp-board__message' style={style}>Reading the model…</div>;
    }

    const tools = (
        <BoardTools
            shown={compiled?.proposed ? shown : undefined}
            onShow={setShown}
            view={view}
            onView={setView}
            fullscreen={host.container?.displayMode === 'fullscreen'}
            onRefresh={host.refresh}
            onToggleFullscreen={host.toggleFullscreen} />
    );
    return (
        <div className='screenplay-mcp-board' style={style}>
            <MenuDropdownOpenProvider>
                <BoardErrorBoundary resetWhenChanged={board} onReset={() => setAttempt(attempt + 1)}>
                    {view === DependencyView.Map ? (
                        <div className='screenplay-dependency-map-host'>
                            <div className='screenplay-dependency-map-toolbar' role='toolbar' aria-label='Model and view'>{tools}</div>
                            <DependencyMapView key={shown} map={board.dependencies} />
                        </div>
                    ) : <EventModelBoard key={attempt} document={model} readOnly showLogo showViewOptions toolbar={tools} canvas={{ chrome: boardChrome }} />}
                </BoardErrorBoundary>
            </MenuDropdownOpenProvider>
            {board.errors > 0 && (
                <div className='screenplay-mcp-board__problems' role='status'>
                    {board.errors === 1 ? '1 error' : `${board.errors} errors`} - the board shows what could be read.
                </div>
            )}
        </div>
    );
};
