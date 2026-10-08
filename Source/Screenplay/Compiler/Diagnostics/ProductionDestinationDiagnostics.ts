// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { ApplicationSyntax } from '../Syntax/Structure';
import { Diagnostic } from './Diagnostic';
import { DiagnosticCodes } from './DiagnosticCodes';

// Routing advice is appended after source validation, as in the C# compiler. It changes no default.
export function productionDestinationDiagnostics(application: ApplicationSyntax): Diagnostic[] {
    const resolver = new AuthoringProductionResolver(application);
    return resolver.slices.flatMap(({ slice }) => slice.commands.flatMap(command => {
        const identifiers = command.properties.filter(property => property.isIdentifier && !property.type.isOptional && !property.type.isCollection);
        if (identifiers.length !== 1) return [];
        return command.produces.filter(production => resolver.isEventProduction(production, slice) && production.inlineEvent === null && production.when === null && production.for === null)
            .map(production => ({ severity: 'information' as const, code: DiagnosticCodes.OmittedProductionDestination,
                message: `Plain 'produces ${production.event}' omits its destination - use 'for ${identifiers[0].name}' to explicitly select the command's identifier.`, location: production.location }));
    }));
}
