// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';

export interface ResponseSourceSymbol {
    readonly property: string;
    readonly location: AnalysisLocation;
}
