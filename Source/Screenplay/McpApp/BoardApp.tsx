// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useRef, useState } from 'react';
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
    const [mapAttempt, setMapAttempt] = useState(0);
    const boardHost = useRef<HTMLDivElement>(null);
    const focusAfterSwitch = useRef(false);
    const changeView = (next: DependencyView) => {
        if (next === view) return;
        if (next === DependencyView.Map) {
            // The pinned board portals menus to body and dismisses them only on an outside
            // pointerdown. Dismiss before hiding it, without resetting the canvas or grid.
            boardHost.current?.ownerDocument.body.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
        }
        focusAfterSwitch.current = true;
        setView(next);
    };
    useEffect(() => {
        if (!focusAfterSwitch.current) return;
        focusAfterSwitch.current = false;
        const selector = view === DependencyView.Board ? '.screenplay-board-view' : '.screenplay-dependency-map-host';
        const label = view === DependencyView.Board ? 'Event model board' : 'Module and feature dependencies';
        boardHost.current?.querySelector<HTMLButtonElement>(`${selector} button[aria-label="${label}"]`)?.focus();
    }, [view]);
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
    if (boards === undefined) {
        return <div className='screenplay-mcp-board__message' style={style}>Reading the model…</div>;
    }
    const boardProblem = boards instanceof Error ? boards : model instanceof Error ? model : undefined;
    if (!(boards instanceof Error) && (board === undefined || model === undefined)) {
        return <div className='screenplay-mcp-board__message' style={style}>Reading the model…</div>;
    }

    const tools = (
        <BoardTools
            shown={compiled?.proposed ? shown : undefined}
            onShow={setShown}
            view={view}
            onView={changeView}
            fullscreen={host.container?.displayMode === 'fullscreen'}
            onRefresh={host.refresh}
            onToggleFullscreen={host.toggleFullscreen} />
    );
    return (
        <div ref={boardHost} className='screenplay-mcp-board' style={style}>
            <MenuDropdownOpenProvider>
                <div className={`screenplay-board-view${view !== DependencyView.Board ? ' is-hidden' : ''}`} aria-hidden={view !== DependencyView.Board} inert={view !== DependencyView.Board}>
                    {boardProblem !== undefined || model === undefined || model instanceof Error ? <>
                        <div className='screenplay-dependency-map-toolbar' role='toolbar' aria-label='Model and view'>{tools}</div>
                        <div className='screenplay-mcp-board__message'>The board could not be drawn: {boardProblem?.message}</div>
                    </> : (
                        <BoardErrorBoundary resetWhenChanged={board} onReset={() => setAttempt(attempt + 1)} tools={tools}>
                            <EventModelBoard key={attempt} document={model} readOnly showLogo showViewOptions toolbar={tools} canvas={{ chrome: boardChrome }} />
                        </BoardErrorBoundary>
                    )}
                </div>
                <div className='screenplay-dependency-map-host' hidden={view !== DependencyView.Map} style={{ display: view !== DependencyView.Map ? 'none' : undefined }}>
                    <div className='screenplay-dependency-map-toolbar' role='toolbar' aria-label='Model and view'>{tools}</div>
                    {board === undefined
                        ? <div className='screenplay-mcp-board__message'>The dependency map is unavailable: {(boards as Error).message}</div>
                        : <BoardErrorBoundary resetWhenChanged={board} onReset={() => setMapAttempt(mapAttempt + 1)} subject='dependency map'>
                            <DependencyMapView key={mapAttempt} map={board.dependencies} />
                        </BoardErrorBoundary>}
                </div>
            </MenuDropdownOpenProvider>
            {board !== undefined && board.errors > 0 && (
                <div className='screenplay-mcp-board__problems' role='status'>
                    {board.errors === 1 ? '1 error' : `${board.errors} errors`} - the board shows what could be read.
                </div>
            )}
        </div>
    );
};
