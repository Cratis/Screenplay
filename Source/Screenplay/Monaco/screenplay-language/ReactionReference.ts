// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';

export interface ReactionReference {
    name: string;
    content: string | null;
    target: { name: string; location: AnalysisLocation; source: string } | null;
}
