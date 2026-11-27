// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { SourceLocation } from '../Diagnostics/SourceLocation';
import type { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

interface Template {
    readonly name: string;
    readonly category?: string | null;
    readonly location: SourceLocation;
}

// Every layout and template names a category so a template picker can group it; a blueprint package build rejects a missing one.
export function validateTemplateCategories(application: ApplicationSyntax, context: ParserContext): void {
    const modules = application.modules ?? [];
    const templates: [string, Template][] = [
        ...(application.layouts ?? []).map((template): [string, Template] => ['layout', template]),
        ...modules.flatMap(module => (module.screenTemplates ?? []).map((template): [string, Template] => ['screen template', template])),
        ...modules.flatMap(module => (module.dialogTemplates ?? []).map((template): [string, Template] => ['dialog template', template]))
    ];
    for (const [kind, template] of templates) {
        if (template.category == null || template.category.trim() === '') context.warning(DiagnosticCodes.TemplateCategoryMissing, `The ${kind} '${template.name}' declares no 'category' - every template needs one so a template picker can group it; a blueprint package build rejects a missing category`, template.location);
    }
}
