// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Memento, WorkspaceConfiguration } from 'vscode';
import type { BoardViewOptions } from '../Webview/BoardMessage';

// What a board is shown with until the person chooses otherwise - what Cratis Studio starts from.
export const defaultViewOptions: BoardViewOptions = {
    detailLevel: 'full', showProperties: false, visualizationMode: 'simplified', density: 'comfortable', textSize: 'medium',
};

const key = 'screenplay.eventModelBoard.viewOptions';

// Keeps the person's board view options in VS Code's storage for the user, so every board - and every
// later session - opens the way they last chose. Anything stored that is not a view option, from an
// older version or edited by hand, falls back to the default for that option alone.
export class ViewOptionsStore {
    constructor(private readonly storage: Memento, private readonly configuration: () => Pick<WorkspaceConfiguration, 'get' | 'update'>) {}

    get options(): BoardViewOptions {
        const stored = this.storedOptions;
        return {
            detailLevel: stored.detailLevel === 'overview' || stored.detailLevel === 'full' ? stored.detailLevel : defaultViewOptions.detailLevel,
            showProperties: typeof stored.showProperties === 'boolean' ? stored.showProperties : defaultViewOptions.showProperties,
            visualizationMode: stored.visualizationMode === 'fillLines' || stored.visualizationMode === 'simplified'
                ? stored.visualizationMode : defaultViewOptions.visualizationMode,
            ...this.spacingOptions,
        };
    }

    private get storedOptions(): Partial<Record<keyof BoardViewOptions, unknown>> {
        const value = this.storage.get<unknown>(key);
        return typeof value === 'object' && value !== null && !Array.isArray(value) ? value as Partial<Record<keyof BoardViewOptions, unknown>> : {};
    }

    private get spacingOptions(): Pick<BoardViewOptions, 'density' | 'textSize'> {
        const configuration = this.configuration();
        const density = configuration.get<unknown>('density');
        const textSize = configuration.get<unknown>('textSize');
        return {
            density: density === 'compact' || density === 'comfortable' || density === 'spacious' ? density : defaultViewOptions.density,
            textSize: textSize === 'small' || textSize === 'medium' || textSize === 'large' ? textSize : defaultViewOptions.textSize,
        };
    }

    async save(options: BoardViewOptions): Promise<void> {
        const current = this.options;
        const { detailLevel, showProperties, visualizationMode } = options;
        await this.storage.update(key, { detailLevel, showProperties, visualizationMode });
        if (options.density !== current.density) await this.configuration().update('density', options.density, true);
        if (options.textSize !== current.textSize) await this.configuration().update('textSize', options.textSize, true);
    }
}
