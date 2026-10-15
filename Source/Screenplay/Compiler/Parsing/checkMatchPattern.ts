// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { MatchPatternCheck } from './MatchPatternCheck';
import { MatchPatternStatus } from './MatchPatternStatus';
import { validateMatchPattern } from './PortableMatchPattern';

/** Checks a decoded pattern without executing it or constructing a platform RegExp. */
export function checkMatchPattern(pattern: string): MatchPatternCheck {
    const issue = validateMatchPattern(pattern);
    if (issue === undefined) return { status: MatchPatternStatus.Admitted, code: null, construct: null, offset: 0, suggestion: null, description: '' };
    return {
        status: issue.invalid ? MatchPatternStatus.Invalid : MatchPatternStatus.Refused,
        code: issue.invalid ? DiagnosticCodes.InvalidMatchPattern : DiagnosticCodes.PatternOutsidePortableMatchDialect,
        construct: issue.construct,
        offset: issue.offset,
        suggestion: issue.suggestion ?? null,
        description: `${issue.construct} at offset ${issue.offset}. ${issue.suggestion ?? ''}`.trimEnd(),
    };
}
