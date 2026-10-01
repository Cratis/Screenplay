// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Memento } from 'vscode';
import type { BoardViewOptions } from '../Webview/BoardMessage';

// What a board is shown with until the person chooses otherwise - what Cratis Studio starts from.
export const defaultViewOptions: BoardViewOptions = { detailLevel: 'full', showProperties: false, visualizationMode: 'simplified' };

const key = 'screenplay.eventModelBoard.viewOptions';

// Keeps the person's board view options in VS Code's storage for the user, so every board - and every
// later session - opens the way they last chose. Anything stored that is not a view option, from an
// older version or edited by hand, falls back to the default for that option alone.
export class ViewOptionsStore {
    constructor(private readonly storage: Memento) {}

    get options(): BoardViewOptions {
        const stored = this.storage.get<Partial<Record<keyof BoardViewOptions, unknown>>>(key) ?? {};
        return {
            detailLevel: stored.detailLevel === 'overview' || stored.detailLevel === 'full' ? stored.detailLevel : defaultViewOptions.detailLevel,
            showProperties: typeof stored.showProperties === 'boolean' ? stored.showProperties : defaultViewOptions.showProperties,
            visualizationMode: stored.visualizationMode === 'fillLines' || stored.visualizationMode === 'simplified'
                ? stored.visualizationMode : defaultViewOptions.visualizationMode,
        };
    }

    save(options: BoardViewOptions): Thenable<void> {
        return this.storage.update(key, { ...options });
    }
}
