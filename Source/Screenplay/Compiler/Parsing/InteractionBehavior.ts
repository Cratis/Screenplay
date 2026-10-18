// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';

// Named declaration identity, including declarations with no guarded bindings.
export interface InteractionBehavior {
    readonly name: string;
    readonly location: SourceLocation;
}
