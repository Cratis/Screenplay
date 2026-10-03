// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { AnalysisType } from './AnalysisType';
import { CommandResponseSymbol } from './CommandResponseSymbol';

export interface AnalysisCommand {
    readonly name: string;
    readonly location: AnalysisLocation;
    readonly response?: CommandResponseSymbol | null;
    readonly properties: readonly { readonly name: string; readonly type: AnalysisType; readonly isIdentifier: boolean; readonly isGenerated?: boolean; readonly location: AnalysisLocation }[];
}
