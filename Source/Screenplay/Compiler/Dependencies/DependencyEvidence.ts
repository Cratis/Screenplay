// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DependencyKind } from './DependencyKind';
import { DependencyNode } from './DependencyNode';

export interface DependencyEvidence {
    readonly consumer: DependencyNode;
    readonly producer: DependencyNode;
    readonly kind: DependencyKind;
    readonly role: string;
    readonly name: string;
    readonly ambiguous: boolean;
    readonly alternatives: readonly DependencyNode[];
    readonly location: SourceLocation;
    readonly testOnly: boolean;
}
