// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { HostContainer } from './HostContainer';

// How tall the board is when the host leaves it to the view: enough to read a few slices inline.
export const inlineHeight = 640;

// The board fills whatever it is given, so the view decides its height: the whole window in fullscreen, the
// host's fixed height when it sets one, and otherwise the inline height within the host's limit.
export function boardHeight(container: HostContainer | undefined): string {
    if (container?.displayMode === 'fullscreen') {
        return '100vh';
    }
    const dimensions = container?.containerDimensions;
    if (dimensions?.height !== undefined) {
        return `${dimensions.height}px`;
    }
    return `${Math.min(dimensions?.maxHeight ?? inlineHeight, inlineHeight)}px`;
}
