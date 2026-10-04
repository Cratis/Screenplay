// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisCommand } from './AnalysisCommand';
import { AuthoredCommandRoute } from './AuthoredCommandRoute';
import { AuthoredEventSource } from './AuthoredEventSource';
import { AuthoredStream } from './AuthoredStream';

export interface EventSourceAnalysis {
    readonly declarations: readonly AuthoredEventSource[];
    readonly routes: readonly AuthoredCommandRoute[];
    readonly ambiguousCandidates: readonly AuthoredCommandRoute[];
    readonly contexts: ReadonlyMap<number, { readonly command?: AnalysisCommand; readonly route?: AuthoredCommandRoute; readonly source?: AuthoredEventSource; readonly stream?: AuthoredStream }>;
    readonly targets: readonly { readonly name: string; readonly source: AuthoredEventSource; readonly stream: AuthoredStream }[];
    readonly resolve: (source: string, stream: string) => { readonly state: string; readonly source?: AuthoredEventSource; readonly stream?: AuthoredStream };
}
