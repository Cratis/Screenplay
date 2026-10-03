// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { TypeReferenceSymbol } from './TypeReferenceSymbol';

export interface AnalysisType extends TypeReferenceSymbol {
    readonly location: AnalysisLocation;
}
