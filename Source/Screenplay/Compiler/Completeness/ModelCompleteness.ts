// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { CompilationResult } from '../ScreenplayCompiler';
import type { ApplicationSyntax } from '../Syntax/Structure';
import { enumerationPinningDiagnostics } from './EnumerationPinning';

/** Reports selected advisory findings separately from ordinary compilation, only on error-free source.
 * Select enum-pinning, PLAY0671 or all. Other completeness checks remain C#-only and are not synthesized.
 */
export function completenessDiagnostics(compilation: CompilationResult<ApplicationSyntax>, checks: readonly string[]): readonly Diagnostic[] {
    if (!compilation.success || !checks.some(check => check === 'enum-pinning' || check === DiagnosticCodes.UnpinnedStoredEnumeration || check === 'all')) return [];
    return enumerationPinningDiagnostics(compilation.value);
}
