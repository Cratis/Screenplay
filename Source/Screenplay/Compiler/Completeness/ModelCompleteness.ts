// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { CompilationResult } from '../ScreenplayCompiler';
import type { ApplicationSyntax } from '../Syntax/Structure';
import { dataBindingDiagnostics } from './DataBindingCompleteness';
import { enumerationPinningDiagnostics } from './EnumerationPinning';
import { personaDiagnostics } from './PersonaCompleteness';
import { purposeDiagnostics } from './PurposeCompleteness';
import { privilegeDiagnostics } from './PrivilegeCompleteness';

/** Reports selected advisory findings separately from ordinary compilation, only on error-free source.
 * Select data-bindings (PLAY0530/PLAY0531), personas (PLAY0574–PLAY0576), purposes (PLAY0602–PLAY0606),
 * privilege (PLAY0652), enum-pinning (PLAY0671), or all.
 * Other completeness checks remain C#-only and are not synthesized.
 */
export function completenessDiagnostics(compilation: CompilationResult<ApplicationSyntax>, checks: readonly string[]): readonly Diagnostic[] {
    if (!compilation.success) return [];
    const selected = (...names: string[]) => checks.some(check => check === 'all' || names.includes(check));
    return [
        ...(selected('data-bindings', DiagnosticCodes.ConflictingScreenDataBinding, DiagnosticCodes.ScreenDataQueryMismatch) ? dataBindingDiagnostics(compilation.value) : []),
        ...(selected('personas', DiagnosticCodes.PersonaWithoutGate, DiagnosticCodes.GateWithoutPersona, DiagnosticCodes.AmbiguousPersonaCaller) ? personaDiagnostics(compilation.value) : []),
        ...(selected('purposes', DiagnosticCodes.PurposeWithoutBasis, DiagnosticCodes.PersonalDataWithoutPurpose, DiagnosticCodes.SpecialDataWithoutCondition, DiagnosticCodes.CriminalDataWithoutAuthorization, DiagnosticCodes.UnusedPurpose) ? purposeDiagnostics(compilation.value) : []),
        ...(selected('privilege', DiagnosticCodes.ReactionPrivilegeEscalation) ? privilegeDiagnostics(compilation.value) : []),
        ...(selected('enum-pinning', DiagnosticCodes.UnpinnedStoredEnumeration) ? enumerationPinningDiagnostics(compilation.value) : []),
    ];
}
