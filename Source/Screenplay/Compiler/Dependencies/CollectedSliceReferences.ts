// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceReference } from './SliceReference';
import { SharedReference } from './SharedReference';

export interface CollectedSliceReferences {
    readonly references: readonly SliceReference[];
    readonly shared: readonly SharedReference[];
}
