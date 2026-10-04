// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { AnalysisType } from './AnalysisType';
import { AuthoredStream } from './AuthoredStream';

export interface AuthoredEventSource {
    readonly name: string;
    readonly location: AnalysisLocation;
    readonly identifier: AnalysisType | null;
    readonly streams: readonly AuthoredStream[];
    readonly description: string | null;
    readonly id: string | null;
}
