// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MatchPatternReplacement } from './MatchPatternReplacement';
import { MatchPatternStatus } from './MatchPatternStatus';

/** Target-neutral pattern admission and authoring advice; offsets count UTF-16 code units. */
export interface MatchPatternCheck {
    status: MatchPatternStatus;
    code: string | null;
    construct: string | null;
    offset: number;
    suggestion: string | null;
    description: string;
    replacement?: MatchPatternReplacement;
}
