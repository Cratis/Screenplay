// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CompilationResult } from './ScreenplayCompiler';
import { resolveActionSubjectApplication } from './Parsing/ActionSubjectResolution';
import { ApplicationSyntax } from './Syntax/Structure';

/** Resolves an immutable authored tree into its effective action-subject semantics without changing the authored syntax. */
export function resolveActionSubjects(application: ApplicationSyntax): CompilationResult<ApplicationSyntax> {
    return resolveActionSubjectApplication(application);
}
