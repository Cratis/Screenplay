// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyKind, SourceLocation } from '@cratis/screenplay-compiler';

/** One reference, shared by all module, feature and context aggregations. */
export interface DependencyMapEvidence {
    readonly consumer: string;
    readonly producer: string;
    readonly kind: DependencyKind;
    readonly name: string;
    readonly location: SourceLocation;
    readonly ambiguous: boolean;
    readonly alternatives: readonly string[];
    readonly testOnly: boolean;
}
