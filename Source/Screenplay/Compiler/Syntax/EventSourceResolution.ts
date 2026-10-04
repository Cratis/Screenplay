// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventSourceSyntax } from './EventSourceSyntax';
import { EventStreamSyntax } from './EventStreamSyntax';
import { EventSourceResolutionKind } from './EventSourceResolutionKind';

export interface EventSourceResolution {
    readonly kind: EventSourceResolutionKind;
    readonly sources: readonly EventSourceSyntax[];
    readonly streams: readonly EventStreamSyntax[];
}
