// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { InteractionAncestor } from './InteractionAlternatives';

// Each attachment is validated against its own nearest subject after workspace assembly.
export interface InteractionUse {
    readonly behavior: string;
    readonly arguments: readonly { readonly name: string; readonly value: string }[];
    readonly location: SourceLocation;
    readonly ancestors: readonly InteractionAncestor[];
}
