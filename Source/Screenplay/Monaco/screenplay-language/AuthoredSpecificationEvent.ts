// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { AuthoredCommandRoute } from './AuthoredCommandRoute';

export interface AuthoredSpecificationEvent {
    readonly location: AnalysisLocation;
    readonly stream?: AuthoredCommandRoute | null;
    readonly noStream?: { readonly location: AnalysisLocation } | null;
}
