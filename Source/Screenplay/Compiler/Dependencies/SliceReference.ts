// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DependencyKind } from './DependencyKind';

export interface SliceReference {
    readonly name: string;
    readonly targetKind: 'Event' | 'ReadModel' | 'Command' | 'Query' | 'Screen';
    readonly kind: DependencyKind;
    readonly role: string;
    readonly location: SourceLocation;
    readonly timeline: boolean;
}
