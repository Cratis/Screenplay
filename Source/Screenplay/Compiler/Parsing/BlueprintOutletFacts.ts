// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ApplicationSyntax } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import type { SourceLine } from './SourceLine';
import { parseBlueprintReference } from './TemplateReferenceText';

// Only the fact needed for outlet deferral, not a second UI-profile/assignment grammar.
const applicationsWithBlueprints = new WeakSet<ApplicationSyntax>();

export function recordBlueprintOutletFacts(application: ApplicationSyntax, lines: readonly SourceLine[]): void {
    let profileIndent: number | undefined;
    let packagesIndent: number | undefined;
    for (const line of lines) {
        if (line.content.length === 0 || line.content.startsWith('//')) continue;
        if (line.indent <= (profileIndent ?? -1)) { profileIndent = undefined; packagesIndent = undefined; }
        if (pattern('^ui\\s+profile\\s+').test(line.content)) profileIndent = line.indent;
        if (profileIndent !== undefined && line.content === 'packages') packagesIndent = line.indent;
        else if (packagesIndent !== undefined && line.indent > packagesIndent && pattern('^[A-Za-z_]\\w*(?:\\.\\w+)*$').test(line.content)) applicationsWithBlueprints.add(application);
        else if (line.indent <= (packagesIndent ?? -1)) packagesIndent = undefined;
        if (line.content.startsWith('template ') && parseBlueprintReference(line.content.slice('template '.length)) !== undefined) applicationsWithBlueprints.add(application);
    }
}

export function inheritBlueprintOutletFacts(application: ApplicationSyntax, fragments: readonly ApplicationSyntax[]): void {
    if (fragments.some(fragment => usesBlueprintOutlets(fragment))) applicationsWithBlueprints.add(application);
}

export function usesBlueprintOutlets(application: ApplicationSyntax): boolean {
    return applicationsWithBlueprints.has(application);
}
