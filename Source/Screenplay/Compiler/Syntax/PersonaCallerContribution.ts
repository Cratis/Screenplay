// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';

export interface PersonaCallerContribution {
    readonly kind: string;
    readonly value: string;
    readonly type: string | null;
    readonly policy: string;
    readonly location: SourceLocation;
}
