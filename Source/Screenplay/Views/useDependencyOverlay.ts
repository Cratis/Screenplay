// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState, type RefObject } from 'react';
import type { DependencyOverlay } from '@cratis/screenplay-event-models';
import { overlayState, type OverlayEmphasis } from './dependencyOverlay';

interface Identified<TId> { readonly id: TId }
interface ModelFeature<TId> extends Identified<TId> { readonly subFeatures: readonly ModelFeature<TId>[]; readonly slices: readonly Identified<TId>[] }

/** The part of a board model the overlay needs: the ids of its modules, features and slices. */
export interface OverlayModel<TId extends { toString(): string }> {
    readonly collections?: readonly { readonly modules: readonly (Identified<TId> & { readonly features: readonly ModelFeature<TId>[] })[] }[];
}

export interface DependencyOverlayBoardProps<TId> {
    readonly selection: {
        readonly selectedScopeId?: TId;
        readonly onScopeSelected: (id: TId) => void;
        readonly emphasis: (id: TId) => OverlayEmphasis | undefined;
    };
    readonly sliceLinks: readonly { readonly from: TId; readonly to: TId; readonly emphasis: 'within' | 'crossing' }[];
}

function idsOf<TId extends { toString(): string }>(model: OverlayModel<TId>): Map<string, TId> {
    const ids = new Map<string, TId>();
    const add = (id: TId) => ids.set(id.toString(), id);
    const features = (items: readonly ModelFeature<TId>[]) => items.forEach(feature => {
        add(feature.id);
        feature.slices.forEach(slice => add(slice.id));
        features(feature.subFeatures);
    });
    // A model with nothing to read - the board draws what could be read - has no ids to select.
    (model.collections ?? []).forEach(collection => collection.modules.forEach(module => { add(module.id); features(module.features); }));
    return ids;
}

/**
 * The selection and slice links for a board. Selecting a scope again clears it, and so does a model
 * the scope is no longer in. The board's own ids are adapted through the read model, so this needs
 * nothing from the board package.
 */
export function useDependencyOverlay<TId extends { toString(): string }>(overlay: DependencyOverlay, model: OverlayModel<TId> | undefined, boardHost?: RefObject<Element | null>): DependencyOverlayBoardProps<TId> {
    const [selectedId, setSelectedId] = useState<string | undefined>();
    const ids = useMemo(() => model === undefined ? new Map<string, TId>() : idsOf(model), [model]);
    const known = selectedId !== undefined && overlay.scopes.some(scope => scope.id === selectedId) && ids.has(selectedId);
    useEffect(() => { if (selectedId !== undefined && !known) setSelectedId(undefined); }, [selectedId, known]);
    const state = useMemo(() => overlayState(overlay, known ? selectedId : undefined, new Set(ids.keys())), [overlay, selectedId, known, ids]);
    const sliceLinks = useMemo(() => state.links.flatMap(link => {
        const from = ids.get(link.from);
        const to = ids.get(link.to);
        return from === undefined || to === undefined ? [] : [{ from, to, emphasis: link.emphasis }];
    }), [state, ids]);
    useMeasureSliceLinks(sliceLinks.length > 0 ? sliceLinks : undefined, boardHost);
    return useMemo(() => ({
        selection: {
            selectedScopeId: state.selectedId === undefined ? undefined : ids.get(state.selectedId),
            onScopeSelected: (id: TId) => setSelectedId(current => current === id.toString() ? undefined : id.toString()),
            emphasis: (id: TId) => state.emphasis(id.toString()),
        },
        sliceLinks,
    }), [state, sliceLinks]);
}

// Workaround for @cratis/event-models 0.296.2: the canvas attaches its root through an inline callback ref, so
// the root is detached while the links mount and the links measure nothing; they only appear once an unrelated
// class changes under the board. After the root is attached, change a class on it and back in one go, which is
// the signal the links already watch. TODO: remove when the board attaches its root before measuring
// (https://github.com/Cratis/StudioIssues/issues/1258).
function useMeasureSliceLinks(links: readonly unknown[] | undefined, boardHost: RefObject<Element | null> | undefined) {
    useEffect(() => {
        if (links === undefined || boardHost === undefined) return;
        const frame = requestAnimationFrame(() => {
            const column = boardHost.current?.querySelector('[data-slice-col]');
            column?.classList.add('screenplay-measure-slice-links');
            column?.classList.remove('screenplay-measure-slice-links');
        });
        return () => cancelAnimationFrame(frame);
    }, [links, boardHost]);
}
