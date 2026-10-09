// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';

export interface PersonaCallerRefusal {
    readonly policy: string | null;
    readonly reason: string;
    readonly location: SourceLocation;
}
