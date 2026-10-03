// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';

export interface AnalysisSpecification {
    readonly location: AnalysisLocation;
    readonly when: { readonly commandType: string; readonly generatedValues?: readonly { readonly property: string; readonly location: AnalysisLocation }[] } | null;
    readonly thenReturns?: { readonly kind: 'ScalarSpecificationReturnSyntax' | 'RecordSpecificationReturnSyntax'; readonly location: AnalysisLocation } | null;
}
