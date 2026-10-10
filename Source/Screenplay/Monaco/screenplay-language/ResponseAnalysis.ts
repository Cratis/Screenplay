// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisCommand } from './AnalysisCommand';
import { AnalysisDiagnostic } from './AnalysisDiagnostic';
import { AnalysisSpecification } from './AnalysisSpecification';
import { OperationAnalysis } from './OperationAnalysis';

export interface ResponseAnalysis {
    readonly identityDetails?: readonly { readonly name: string; readonly type: string; readonly line: number }[];
    readonly commands: ReadonlyMap<number, AnalysisCommand>;
    readonly specifications: ReadonlyMap<number, AnalysisSpecification>;
    readonly diagnostics: readonly AnalysisDiagnostic[];
    readonly operationProductionLines?: ReadonlySet<number>;
    readonly operations?: OperationAnalysis;
}
