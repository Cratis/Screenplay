// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { PropertySyntax } from '../Syntax/Declarations';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

// Only key-owner and property-shape checks are shared. Lookup resolution remains C# only.
export function validateReadModelKeys(application: ApplicationSyntax, context: ParserContext): void {
    const models = new AuthoringProductionResolver(application).slices.flatMap(({ slice }) => slice.readModels);
    const allowed = new Set(models.flatMap(model => model.properties));
    class KeyOwners extends ScreenplaySyntaxWalker {
        visitProperty(syntax: PropertySyntax): void {
            if (syntax.isKey && !allowed.has(syntax)) {
                context.error(DiagnosticCodes.InvalidReadModelKey, 'The key modifier is only valid on top-level read-model properties.', syntax.location);
            }
            super.visitProperty(syntax);
        }
    }
    new KeyOwners().visitApplication(application);
    for (const model of models) {
        const keys = model.properties.filter(property => property.isKey);
        const names = new Set<string>();
        for (const property of keys) {
            if (names.has(property.name)) context.error(DiagnosticCodes.InvalidReadModelKey, `Read-model key part '${property.name}' must be declared exactly once.`, property.location);
            names.add(property.name);
        }
        for (const property of keys) {
            if (keys.length > 1 && application.types.some(type => type.name === property.type.name)) {
                context.error(DiagnosticCodes.InvalidReadModelKey, 'A read-model key part must be required and noncollection, cannot combine modifiers, and a multipart key must have scalar parts.', property.location);
            }
        }
    }
}
