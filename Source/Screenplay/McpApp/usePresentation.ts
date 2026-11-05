// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { defaultDetailsVisibilityState, type EventModelPresentation, type EventModelPresentationChange } from '@cratis/event-models';

export const defaultPresentation: EventModelPresentation = {
    detailLevel: 'full',
    visualizationMode: 'simplified',
    detailsVisibility: defaultDetailsVisibilityState,
    showStatuses: false,
    density: 'comfortable',
    textSize: 'medium',
};

// The pinned host protocol has no preference storage contract. Choices live only in this mounted app.
export function usePresentation(): [EventModelPresentation, EventModelPresentationChange] {
    return useState(defaultPresentation);
}
