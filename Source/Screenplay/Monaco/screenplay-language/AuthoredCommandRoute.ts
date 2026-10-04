// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';

export interface AuthoredCommandRoute {
    readonly eventSource: string;
    readonly stream: string;
    readonly location: AnalysisLocation;
    readonly referenceLocation?: AnalysisLocation;
    readonly referenceLength?: number;
    readonly streamId: { readonly location: AnalysisLocation; readonly source: unknown } | null;
    readonly propertyCandidate: unknown;
}
