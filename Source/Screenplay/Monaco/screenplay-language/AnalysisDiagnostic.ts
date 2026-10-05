// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';

export interface AnalysisDiagnostic {
    readonly code: string;
    readonly message: string;
    readonly severity: 'error' | 'warning' | 'information';
    readonly location: AnalysisLocation;
}
