// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

// A read model has an identity; only primitives, concepts, top-level types and imported names
// belong to the reusable value-type namespace. Validate after file composition.
export function validateReadModelMemberTypes(application: ApplicationSyntax, context: ParserContext): void {
    const types = new Set([
        'Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime',
        ...application.concepts.map(concept => concept.name),
        ...application.types.map(type => type.name),
        ...application.imports.map(imported => imported.qualifiedName.split('.').at(-1)!),
    ]);
    const slices = new AuthoringProductionResolver(application).slices.map(({ slice }) => slice);
    const models = slices.flatMap(slice => slice.readModels);
    const readModels = new Set([
        ...models.map(model => model.name),
        ...slices.flatMap(slice => (dependencySourcesOf(slice).reducers ?? []).map(reducer => reducer.readModel)),
        ...slices.flatMap(slice => slice.projections.flatMap(projection => {
            const variants = projection.blocks.filter(block => block.kind === 'ProjectionVariantSyntax');
            return variants.length > 0 ? variants.map(variant => variant.name) : [projection.readModel ?? projection.name];
        })),
    ]);
    for (const model of models) {
        for (const property of model.properties.filter(property => !types.has(property.type.name) && readModels.has(property.type.name))) {
            context.error(DiagnosticCodes.InvalidSemanticBinding,
                `Read model '${model.name}' member '${property.name}' references unsupported type '${property.type.name}'. Use a primitive, concept or top-level 'type'; read-model shapes and slice-local 'type' declarations are not member types.`,
                property.type.location);
        }
    }
}
