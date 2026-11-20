// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { ScreenThumbnail, readThumbnailDocument, screenEditorRuntimeForProfile, type ScreenEditorRuntime } from '@cratis/scene.editor';
import type { BoardScreens } from '@cratis/event-models';
import { boardScreenDocumentFor, hasBoardScreens, type BoardScreenDocuments } from './boardScreens';

// One runtime for the whole board: the default UI profile over the blueprint packages Scene previews with.
// A .play document declares no stored profile - it is a model, not an application that has been dressed - so
// the screen is drawn in the profile's own appearance, exactly as Studio's shared Viewer draws it.
// Undefined until a board actually has a screen, so a model without one builds nothing.
export const useScreenplayScreenRuntime = (screens: BoardScreenDocuments): ScreenEditorRuntime | undefined =>
    useMemo(() => hasBoardScreens(screens) ? screenEditorRuntimeForProfile(undefined) : undefined, [screens]);

// How a read-only Screenplay board draws its screens. The documents were compiled with the board, so nothing
// is read from disk or fetched while the board draws; a role whose screen is missing or unreadable keeps the
// board's own placeholder window rather than showing an error.
export const useScreenplayBoardScreens = (screens: BoardScreenDocuments, runtime: ScreenEditorRuntime | undefined): BoardScreens =>
    useMemo<BoardScreens>(() => ({
        renderThumbnail: ({ sliceId, actorId, windowSize, openTitle, fallback }) => {
            if (!runtime) return fallback;
            const document = readThumbnailDocument(boardScreenDocumentFor(screens, sliceId, actorId));
            if (!document) return fallback;
            return (
                <ScreenThumbnail
                    document={document}
                    runtime={runtime}
                    windowSize={windowSize}
                    openTitle={openTitle}
                    placeholder={fallback}
                />
            );
        },
    }), [screens, runtime]);
