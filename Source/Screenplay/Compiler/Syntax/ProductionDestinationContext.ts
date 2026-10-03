// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionResolver } from './AuthoringProductionResolver';
import { SliceSyntax } from './Structure';

/** Optional assembled declaration context for standalone production references. */
export interface ProductionDestinationContext {
    readonly resolver: AuthoringProductionResolver;
    readonly slice: SliceSyntax;
}
