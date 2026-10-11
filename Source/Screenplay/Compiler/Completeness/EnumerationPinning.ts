// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { EventSyntax } from '../Syntax/Declarations';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax } from '../Syntax/Structure';

export function enumerationPinningDiagnostics(application: ApplicationSyntax): readonly Diagnostic[] {
    const diagnostics: Diagnostic[] = [];
    const names = new Set<string>();
    const add = (name: string): void => {
        if (names.has(name)) return;
        names.add(name);
        for (const type of application.types.filter(type => type.name === name)) for (const property of type.properties) add(property.type.name);
    };
    class StoredTypes extends ScreenplaySyntaxWalker {
        override visitEvent(syntax: EventSyntax): void {
            for (const property of syntax.properties) add(property.type.name);
            super.visitEvent(syntax);
        }
    }
    new StoredTypes().visitApplication(application);
    for (const concept of application.concepts.filter(concept => concept.type === 'Enum' && concept.valueNumbers == null && names.has(concept.name))) {
        diagnostics.push({ severity: 'information', code: DiagnosticCodes.UnpinnedStoredEnumeration, message: `Enum '${concept.name}' is used by an event property; use 'Number every member by its current position' to pin stored member identity before reordering or inserting members`, location: concept.location });
    }
    return diagnostics;
}
