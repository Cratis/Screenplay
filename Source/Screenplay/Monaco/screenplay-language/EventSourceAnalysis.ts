// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { AnalysisType } from './AnalysisType';
import { AnalysisCommand } from './AnalysisCommand';

export interface AuthoredStream {
    readonly name: string;
    readonly location: AnalysisLocation;
    readonly streamId: AnalysisType | null;
    readonly description: string | null;
    readonly id: string | null;
}

export interface AuthoredEventSource {
    readonly name: string;
    readonly location: AnalysisLocation;
    readonly identifier: AnalysisType | null;
    readonly streams: readonly AuthoredStream[];
    readonly description: string | null;
    readonly id: string | null;
}

export interface AuthoredCommandRoute {
    readonly eventSource: string;
    readonly stream: string;
    readonly location: AnalysisLocation;
    readonly referenceLocation?: AnalysisLocation;
    readonly referenceLength?: number;
    readonly streamId: { readonly location: AnalysisLocation; readonly source: unknown } | null;
    readonly propertyCandidate: unknown;
}

export interface EventSourceAnalysis {
    readonly declarations: readonly AuthoredEventSource[];
    readonly routes: readonly AuthoredCommandRoute[];
    readonly ambiguousCandidates: readonly AuthoredCommandRoute[];
    readonly contexts: ReadonlyMap<number, { readonly command?: AnalysisCommand; readonly route?: AuthoredCommandRoute; readonly source?: AuthoredEventSource; readonly stream?: AuthoredStream }>;
    readonly targets: readonly { readonly name: string; readonly source: AuthoredEventSource; readonly stream: AuthoredStream }[];
    readonly resolve: (source: string, stream: string) => { readonly state: string; readonly source?: AuthoredEventSource; readonly stream?: AuthoredStream };
}
