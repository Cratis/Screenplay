// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { timelineOrderFindings, type ApplicationSyntax, type Diagnostic } from '@cratis/screenplay-compiler';
import { SliceAnnotationDocument } from '../Document/EventModelDocument';
import { SliceScope } from './SliceScope';

// Only present findings the compilation actually reported: ordering-root gating and narrowed diagnostics belong
// to the compiler and host, while the structured consumer scope supplies the board's stable slice identity.
export function timelineAnnotations(application: ApplicationSyntax, diagnostics: readonly Diagnostic[]): ReadonlyMap<string, SliceAnnotationDocument[]> {
    const annotations = new Map<string, SliceAnnotationDocument[]>();
    const reported = new Set(diagnostics.map(diagnosticKey));
    for (const finding of timelineOrderFindings(application)) {
        const diagnostic = finding.diagnostic;
        if (!reported.has(diagnosticKey(diagnostic))) continue;

        const scope = finding.consumerScope;
        const container = scope.slice(1, -1).reduce((outer, name) => outer.feature(name), SliceScope.module(scope[0]));
        const id = container.slice(scope[scope.length - 1]).id;
        const entries = annotations.get(id) ?? [];
        entries.push({ severity: 'information', code: diagnostic.code, message: diagnostic.message });
        annotations.set(id, entries);
    }
    return annotations;
}

function diagnosticKey(diagnostic: Diagnostic): string {
    return JSON.stringify([diagnostic.code, diagnostic.location.path, diagnostic.location.line, diagnostic.location.column]);
}
