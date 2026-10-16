// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PortableMatchCheck } from './PortableMatchCheck';
import { PortableMatchStatus } from './PortableMatchStatus';
import { recognizeLegacyTextRule } from './LegacyTextRulePatterns';
import { validateMatchPattern } from './PortableMatchPattern';

/** Checks a decoded pattern without executing it or constructing a platform RegExp. */
export function checkPortableMatchPattern(pattern: string): PortableMatchCheck {
    const replacement = recognizeLegacyTextRule(pattern);
    if (replacement !== undefined) {
        const spelling = replacement.kind === 'Equal' ? '==' : replacement.kind === 'NotEqual' ? '!=' : replacement.kind === 'NotContains' ? 'not contains' : 'ends with';
        const suggestion = `Use '${spelling}' with the literal text operand instead.`;
        return { status: PortableMatchStatus.Deprecated, code: DiagnosticCodes.DeprecatedStudioTextRulePattern, construct: 'Studio text rule', offset: 0, suggestion, description: `This Studio lookaround pattern is deprecated. ${suggestion}`, replacement };
    }
    const issue = validateMatchPattern(pattern);
    if (issue === undefined) return { status: PortableMatchStatus.Admitted, code: null, construct: null, offset: 0, suggestion: null, description: '' };
    return {
        status: issue.invalid ? PortableMatchStatus.Invalid : PortableMatchStatus.Refused,
        code: issue.invalid ? DiagnosticCodes.InvalidMatchPattern : DiagnosticCodes.PatternOutsidePortableMatchDialect,
        construct: issue.construct,
        offset: issue.offset,
        suggestion: issue.suggestion ?? null,
        description: `${issue.construct} at offset ${issue.offset}. ${issue.suggestion ?? ''}`.trimEnd(),
    };
}
