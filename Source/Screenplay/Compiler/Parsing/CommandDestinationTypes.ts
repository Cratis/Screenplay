// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax } from '../Syntax/Commands';
import { TypeRefSyntax } from '../Syntax/Declarations';
import { ProductionDestinationContext } from '../Syntax/ProductionDestinationContext';
import { implicitDestination } from '../Syntax/ProductionDestinations';
import { ProducesSyntax } from '../Syntax/Reactions';
import { ApplicationSyntax } from '../Syntax/Structure';
import { uniqueByName } from './ResponseValidator';

export function commandPathType(command: CommandSyntax, path: string, application: ApplicationSyntax): TypeRefSyntax | null {
    const composites = uniqueByName(application.types);
    let fields = command.properties;
    let result: TypeRefSyntax | null = null;
    let optional = false;
    let collection = false;
    for (const segment of path.split('.')) {
        const matches = fields.filter(field => field.name === segment);
        if (matches.length !== 1) return null;
        const type = matches[0].type;
        optional ||= type.isOptional;
        collection ||= type.isCollection;
        result = { ...type, isOptional: optional, isCollection: collection };
        fields = composites.get(type.name)?.properties ?? [];
    }
    return result;
}

export function commandDestinationType(command: CommandSyntax, produced: ProducesSyntax, application: ApplicationSyntax, context: ProductionDestinationContext): TypeRefSyntax | null {
    const destination = produced.for?.kind === 'PathExpressionSyntax' ? produced.for.path : implicitDestination(command, produced, context);
    return destination === 'new event source'
        ? command.properties.find(property => property.isGenerated && property.isIdentifier)?.type ?? { kind: 'TypeRefSyntax', name: 'Uuid', isOptional: false, isCollection: false, location: produced.location }
        : destination === undefined ? null : commandPathType(command, destination, application);
}
