// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisCommand } from './AnalysisCommand';
import { AnalysisDiagnostic } from './AnalysisDiagnostic';
import { AnalysisSpecification } from './AnalysisSpecification';

export interface ResponseAnalysis {
    readonly commands: ReadonlyMap<number, AnalysisCommand>;
    readonly specifications: ReadonlyMap<number, AnalysisSpecification>;
    readonly diagnostics: readonly AnalysisDiagnostic[];
    readonly operationProductionLines?: ReadonlySet<number>;
}
