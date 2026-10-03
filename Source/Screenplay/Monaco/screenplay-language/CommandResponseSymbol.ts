// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { ResponseFieldSymbol } from './ResponseFieldSymbol';
import { ResponseSourceSymbol } from './ResponseSourceSymbol';

export type CommandResponseSymbol =
    | { readonly kind: 'ScalarCommandResponseSyntax'; readonly source: ResponseSourceSymbol; readonly location: AnalysisLocation }
    | { readonly kind: 'RecordCommandResponseSyntax'; readonly fields: readonly ResponseFieldSymbol[]; readonly location: AnalysisLocation };
