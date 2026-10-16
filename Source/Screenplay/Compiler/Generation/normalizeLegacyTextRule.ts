// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { recognizeLegacyTextRule } from '../Parsing/LegacyTextRulePatterns';
import { GenerationValidationRule } from './GenerationValidationRule';
import { GenerationValidationRuleKind as Kind } from './GenerationValidationRuleKind';

export function normalizeLegacyTextRule(rule: GenerationValidationRule): GenerationValidationRule {
    const replacement = rule.kind === Kind.Matches && typeof rule.operand === 'string' ? recognizeLegacyTextRule(rule.operand) : undefined;
    return replacement === undefined ? rule : { ...rule, kind: Kind[replacement.kind], operand: replacement.value };
}
