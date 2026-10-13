// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisType } from './AnalysisType';

export interface ContinuationResponseField {
    readonly name: string;
    readonly type: AnalysisType;
    readonly command: string;
}
