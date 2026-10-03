// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { AnalysisType } from './AnalysisType';
import { ResponseSourceSymbol } from './ResponseSourceSymbol';

export interface ResponseFieldSymbol {
    readonly name: string;
    readonly type: AnalysisType | null;
    readonly source: ResponseSourceSymbol;
    readonly location: AnalysisLocation;
}
