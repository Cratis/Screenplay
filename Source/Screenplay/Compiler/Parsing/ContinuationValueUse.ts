// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';

// Authoring facts only: continuations do not enter the executable semantic model.
export interface ContinuationValueUse {
    readonly text: string;
    readonly location: SourceLocation;
    readonly scope: readonly string[];
    readonly success: readonly string[];
    readonly failure: readonly string[];
    readonly locals: readonly string[];
    readonly parameters: readonly string[];
}
