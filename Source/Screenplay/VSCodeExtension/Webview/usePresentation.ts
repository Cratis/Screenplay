// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { defaultDetailsVisibilityState, EventModelPresentation } from '@cratis/event-models';
import type { BoardViewOptions, ExtensionToBoardMessage } from './BoardMessage';
import { vscode } from './vscodeApi';

// How the board is first shown: what Cratis Studio starts from - without statuses, which a model drawn
// from its text has nothing to provide.
export const defaultPresentation: EventModelPresentation = {
    detailLevel: 'full',
    visualizationMode: 'simplified',
    detailsVisibility: defaultDetailsVisibilityState,
    showStatuses: false,
};

const toPresentation = (options: BoardViewOptions): EventModelPresentation => ({
    ...defaultPresentation,
    detailLevel: options.detailLevel,
    visualizationMode: options.visualizationMode,
    detailsVisibility: { ...defaultDetailsVisibilityState, global: options.showProperties },
});

const toViewOptions = (presentation: EventModelPresentation): BoardViewOptions => ({
    detailLevel: presentation.detailLevel,
    showProperties: presentation.detailsVisibility.global,
    visualizationMode: presentation.visualizationMode,
});

// How the board is presented - detail level, properties and how connections are drawn. The extension
// keeps it for the person: it sends what they last chose when the board is ready, and is told of every
// change, which it saves and shows on the other open boards.
export function usePresentation(): [EventModelPresentation, (presentation: EventModelPresentation) => void] {
    const [presentation, setPresentation] = useState(defaultPresentation);

    useEffect(() => {
        const receive = (event: MessageEvent<ExtensionToBoardMessage>) => {
            if (event.data.type === 'viewOptions') {
                setPresentation(toPresentation(event.data.options));
            }
        };
        window.addEventListener('message', receive);
        return () => window.removeEventListener('message', receive);
    }, []);

    const change = (next: EventModelPresentation) => {
        setPresentation(next);
        vscode.postMessage({ type: 'viewOptionsChanged', options: toViewOptions(next) });
    };
    return [presentation, change];
}
