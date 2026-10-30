// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyOverlay } from '@cratis/screenplay-event-models';

export type OverlayEmphasis = 'selected' | 'dependency' | 'dependant' | 'dimmed';

export interface OverlayLink {
    readonly from: string;
    readonly to: string;
    readonly emphasis: 'within' | 'crossing';
}

export interface OverlayState {
    readonly selectedId?: string;
    readonly emphasis: (id: string) => OverlayEmphasis | undefined;
    readonly links: readonly OverlayLink[];
}

const nothingSelected: OverlayState = { emphasis: () => undefined, links: [] };

/**
 * What the board shows for a selected scope. A selection the overlay does not know resolves to none,
 * a slice that is both a dependency and a dependant counts as a dependency, and links with an end the
 * board does not draw are left out.
 */
export function overlayState(overlay: DependencyOverlay, selectedId: string | undefined, drawnIds: ReadonlySet<string>): OverlayState {
    const scope = selectedId === undefined ? undefined : overlay.scopes.find(candidate => candidate.id === selectedId);
    if (scope === undefined) return nothingSelected;
    const selected = new Set(scope.selected);
    const dependencies = new Set(scope.dependencies);
    const dependants = new Set(scope.dependants);
    const ancestors = new Set(scope.ancestors);
    return {
        selectedId: scope.id,
        emphasis: id => selected.has(id) ? 'selected' : dependencies.has(id) ? 'dependency' : dependants.has(id) ? 'dependant' : ancestors.has(id) ? undefined : 'dimmed',
        links: scope.links.filter(link => drawnIds.has(link.from) && drawnIds.has(link.to)).map(link => ({ from: link.from, to: link.to, emphasis: link.crossing ? 'crossing' : 'within' })),
    };
}
