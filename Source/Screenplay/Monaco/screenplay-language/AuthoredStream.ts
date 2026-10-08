// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { AnalysisType } from './AnalysisType';

export interface AuthoredStream {
    readonly name: string;
    readonly location: AnalysisLocation;
    readonly streamId: AnalysisType | null;
    readonly streamIdParts: readonly { readonly name: string; readonly type: AnalysisType; readonly location: AnalysisLocation }[];
    readonly directiveLocations?: Readonly<Record<string, AnalysisLocation>>;
    readonly description: string | null;
    readonly id: string | null;
}
