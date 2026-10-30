// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventModelDocument, FeatureDocument, SliceAnnotationDocument, SliceDocument } from '../Document/EventModelDocument';

// Append host findings without replacing authored specification routes or mutating the board document.
export function withSliceAnnotations(document: EventModelDocument, annotations: ReadonlyMap<string, SliceAnnotationDocument[]>): EventModelDocument {
    const sliceWithAnnotations = (slice: SliceDocument): SliceDocument => {
        const extra = annotations.get(slice.id);
        return extra?.length ? { ...slice, annotations: [...(slice.annotations ?? []), ...extra] } : slice;
    };
    const featureWithAnnotations = (feature: FeatureDocument): FeatureDocument => ({
        ...feature,
        slices: feature.slices.map(sliceWithAnnotations),
        subFeatures: feature.subFeatures.map(featureWithAnnotations),
    });
    return {
        ...document,
        collections: document.collections.map(collection => ({
            ...collection,
            modules: collection.modules.map(module => ({ ...module, features: module.features.map(featureWithAnnotations) })),
        })),
    };
}
