// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationRuleKind } from '../Syntax/Commands';

/** An equivalent ordinal text comparison. */
export interface MatchPatternReplacement {
    kind: ValidationRuleKind;
    value: string;
}
