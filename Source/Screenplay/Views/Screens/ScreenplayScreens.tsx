// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { ScreenThumbnail, readThumbnailDocument, screenEditorRuntimeForProfile, type ScreenEditorRuntime } from '@cratis/scene.editor';
import type { BoardScreens } from '@cratis/event-models';
import { hasScreenDocuments, type ScreenDocuments } from '@cratis/screenplay-event-models';
import { boardScreenAppearance } from './boardScreenAppearance';
import { screenToDraw } from './screenToDraw';

// One Scene runtime for the whole board: the blueprint packages Scene previews with, in the board's own theme.
// A .play document names no stored UI profile, so the default one is what a screen is drawn against - the same
// profile the Screenplay toolchain designs a screen in. Nothing is built until a board actually has a screen.
export function useScreenplayScreenRuntime(screens: ScreenDocuments, dark: boolean): ScreenEditorRuntime | undefined {
    // Built from whether there are screens at all, never from the documents: a .play is recompiled on every
    // keystroke, and rebuilding the whole package registry for each new record would make typing cost a board.
    const any = hasScreenDocuments(screens);
    return useMemo(
        () => any ? screenEditorRuntimeForProfile(undefined, undefined, boardScreenAppearance(dark)) : undefined,
        [any, dark]);
}

// How a read-only board draws its screens. The documents were compiled with the board document, so nothing is
// fetched or compiled while the board draws, and a cell with no readable screen keeps the board's own
// placeholder window rather than showing an error.
export function useScreenplayBoardScreens(screens: ScreenDocuments, runtime: ScreenEditorRuntime | undefined): BoardScreens {
    return useMemo<BoardScreens>(() => ({
        renderThumbnail: ({ sliceId, actorId, windowSize, openTitle, fallback }) => {
            const document = screenToDraw(screens, sliceId, actorId, readThumbnailDocument, runtime !== undefined);
            if (document === undefined || runtime === undefined) return fallback;
            return (
                <ScreenThumbnail
                    document={document}
                    runtime={runtime}
                    windowSize={windowSize}
                    openTitle={openTitle}
                    placeholder={fallback} />
            );
        },
    }), [screens, runtime]);
}
