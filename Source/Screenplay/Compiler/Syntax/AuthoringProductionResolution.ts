// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionDeclaration } from './AuthoringProductionDeclaration';
import { AuthoringProductionKind } from './AuthoringProductionKind';

export interface AuthoringProductionResolution {
    readonly kind: AuthoringProductionKind;
    readonly declaration: AuthoringProductionDeclaration | null;
    readonly candidates: readonly AuthoringProductionDeclaration[];
}
