// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { defaultEventModelPresentation, EventModelPresentation } from '@cratis/event-models';
import { vscode } from './vscodeApi';

interface BoardState {
    readonly presentation?: Partial<EventModelPresentation>;
}

// How the board is presented - detail level, properties and how connections are drawn - starting from
// what Cratis Studio starts from, and kept by VS Code for as long as the editor is open.
export function usePresentation(): [EventModelPresentation, (change: (current: EventModelPresentation) => EventModelPresentation) => void] {
    const [presentation, setPresentation] = useState<EventModelPresentation>(() => ({
        ...defaultEventModelPresentation,
        visualizationMode: 'simplified',
        ...(vscode.getState() as BoardState | undefined)?.presentation,
    }));

    useEffect(() => vscode.setState({ presentation } satisfies BoardState), [presentation]);

    return [presentation, change => setPresentation(current => change(current))];
}
