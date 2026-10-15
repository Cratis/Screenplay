// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GenerationValidationRuleKind } from './GenerationValidationRuleKind';

/** Shape rules accept safe integer lengths or exact decimal strings; random bounds use their separate entry point. */
export interface GenerationValidationRule {
    readonly kind: GenerationValidationRuleKind;
    readonly operand?: string | number;
    readonly severity?: string;
}
