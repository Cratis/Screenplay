// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { AuthoredSpecificationEvent } from './AuthoredSpecificationEvent';

export interface AnalysisSpecification {
    readonly location: AnalysisLocation;
    readonly name?: string;
    readonly parameters?: readonly { readonly name: string; readonly type: { readonly name: string; readonly isOptional: boolean; readonly isCollection: boolean } }[];
    readonly cases?: readonly { readonly name: string; readonly values: readonly { readonly property: string; readonly source: unknown }[] }[];
    readonly when: { readonly commandType: string; readonly generatedValues?: readonly { readonly property: string; readonly location: AnalysisLocation }[] } | null;
    readonly thenEvents?: readonly AuthoredSpecificationEvent[];
    readonly thenReturns?: { readonly kind: 'ScalarSpecificationReturnSyntax' | 'RecordSpecificationReturnSyntax'; readonly location: AnalysisLocation } | null;
}
