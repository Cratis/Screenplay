// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SpecificationCallerSyntax } from './Specifications';
import { PersonaCallerContribution } from './PersonaCallerContribution';
import { PersonaCallerRefusal } from './PersonaCallerRefusal';

export interface PersonaCallerResult {
    readonly caller: SpecificationCallerSyntax | null;
    readonly contributions: readonly PersonaCallerContribution[];
    readonly refusal: PersonaCallerRefusal | null;
}
