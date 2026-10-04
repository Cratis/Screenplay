// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { App } from '@modelcontextprotocol/ext-apps/app-with-deps';
import { documentsSignature } from './documentsSignature';
import type { HostContainer } from './HostContainer';
import { toVisualizedModel } from './toVisualizedModel';
import type { VisualizedModel } from './VisualizedModel';

// The tool the board is the view of, and that it calls again to refresh.
export const visualizeTool = 'visualize-model';

// How often the board looks for changes to the model on disk while it is on screen.
export const watchIntervalMilliseconds = 2000;

export interface Host {
    readonly model?: VisualizedModel | Error;
    readonly container?: HostContainer;
    // Draws the board again from the server, with the arguments it was first drawn with. Left out when the
    // host does not let views call the server.
    readonly refresh?: () => void;
    // Switches between inline and fullscreen. Left out when the host offers no fullscreen.
    readonly toggleFullscreen?: () => void;
}

// The board's connection to the host that shows it: the model the visualize-model tool sent, where the
// board is shown, and what the host lets it ask for.
export function useHost(): Host {
    const app = useMemo(() => new App({ name: 'Screenplay event model board', version: '1.0.0' }, { availableDisplayModes: ['inline', 'fullscreen'] }), []);
    const [model, setModel] = useState<VisualizedModel | Error>();
    const [container, setContainer] = useState<HostContainer>();
    const [toolArguments, setToolArguments] = useState<Record<string, unknown>>({});
    const [connected, setConnected] = useState(false);
    const drawn = useRef<string | undefined>(undefined);

    // Notes what the board now draws, so the watcher can tell a change from the same model again.
    const remember = useCallback((next: VisualizedModel | Error) => {
        drawn.current = next instanceof Error ? undefined : documentsSignature(next);
        return next;
    }, []);

    useEffect(() => {
        app.ontoolinput = params => setToolArguments(params.arguments ?? {});
        app.ontoolresult = result => setModel(remember(toVisualizedModel(result)));
        app.onhostcontextchanged = () => setContainer(app.getHostContext());
        app.onteardown = async () => ({});
        app.connect()
            .then(() => {
                setContainer(app.getHostContext());
                setConnected(true);
            })
            .catch(error => setModel(error instanceof Error ? error : new Error(String(error))));
    }, [app, remember]);

    const refresh = useCallback(() => {
        app.callServerTool({ name: visualizeTool, arguments: toolArguments })
            .then(result => setModel(remember(toVisualizedModel(result))))
            .catch(error => setModel(error instanceof Error ? error : new Error(String(error))));
    }, [app, remember, toolArguments]);

    // The board follows the model on disk: while it is visible it asks for the application as it is now and
    // draws it when it differs from what is drawn. A proposal on screen stays until the files change, and
    // then the board shows the application as it is. A failed look changes nothing; the refresh button
    // reports the error.
    const canWatch = connected && app.getHostCapabilities()?.serverTools !== undefined;
    useEffect(() => {
        if (!canWatch) {
            return undefined;
        }

        let looking = false;
        const look = () => {
            if (looking || document.visibilityState === 'hidden') {
                return;
            }

            looking = true;
            app.callServerTool({ name: visualizeTool, arguments: {} })
                .then(result => {
                    const current = toVisualizedModel(result);
                    if (!(current instanceof Error) && documentsSignature(current) !== drawn.current) {
                        setModel(remember(current));
                    }
                })
                .catch(() => undefined)
                .finally(() => { looking = false; });
        };
        const timer = window.setInterval(look, watchIntervalMilliseconds);
        return () => window.clearInterval(timer);
    }, [app, canWatch, remember]);

    const toggleFullscreen = useCallback(() => {
        const mode = container?.displayMode === 'fullscreen' ? 'inline' : 'fullscreen';
        app.requestDisplayMode({ mode }).then(result => setContainer(current => ({ ...current, displayMode: result.mode }))).catch(() => undefined);
    }, [app, container?.displayMode]);

    const hostCapabilities = connected ? app.getHostCapabilities() : undefined;
    const displayModes = (app.getHostContext()?.availableDisplayModes ?? []) as readonly string[];
    return {
        model,
        container,
        refresh: hostCapabilities?.serverTools ? refresh : undefined,
        toggleFullscreen: displayModes.includes('fullscreen') ? toggleFullscreen : undefined,
    };
}
