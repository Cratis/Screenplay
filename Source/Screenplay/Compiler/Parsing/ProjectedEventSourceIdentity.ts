// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventSyntax, ReadModelSyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { KeySyntax, ProjectionSyntax } from '../Syntax/Projections';
import { QuerySyntax } from '../Syntax/Queries';
import { SliceSyntax } from '../Syntax/Structure';
import { RefusalDeclarations } from './RefusalDeclarations';

export function projectedEventSourceIdentity(event: EventSyntax, declarations: RefusalDeclarations): TypeRefSyntax | undefined {
    const keys = declarations.slices.flatMap(({ slice }) => slice.projections
        .filter(projection => projectsEventSource(projection, event, slice, declarations))
        .map(projection => identityType(projection, slice, declarations)));
    return uniqueIdentity(keys, declarations);
}

function uniqueIdentity(keys: readonly (TypeRefSyntax | undefined)[], declarations: RefusalDeclarations): TypeRefSyntax | undefined {
    const scalar = keys.filter((type): type is TypeRefSyntax => type !== undefined && isRequiredScalar(type, declarations));
    if (scalar.length !== keys.length) return undefined;
    const types = new Map(scalar.map(type => [type.name, type]));
    return types.size === 1 ? [...types.values()][0] : undefined;
}

function isRequiredScalar(type: TypeRefSyntax, declarations: RefusalDeclarations): boolean {
    return !type.isOptional && !type.isCollection && !declarations.types.has(type.name);
}

function projectsEventSource(projection: ProjectionSyntax, event: EventSyntax, slice: SliceSyntax, declarations: RefusalDeclarations): boolean {
    return projection.readModel !== null && projection.blocks.some(block => block.kind === 'FromSyntax' && block.events.some(spec =>
        declarations.event(spec.event, slice) === event && (spec.key?.kind === 'EventSourceIdExpressionSyntax' || (spec.key == null && isEventSourceKey(block.key)))));
}

function isEventSourceKey(key: KeySyntax | null | undefined): boolean {
    return key == null || (key.kind === 'ExpressionKeySyntax' && key.expression.kind === 'EventSourceIdExpressionSyntax');
}

function identityType(projection: ProjectionSyntax, owner: SliceSyntax, declarations: RefusalDeclarations): TypeRefSyntax | undefined {
    const declaration = declarations.resolve(projection.readModel!, owner, slice => slice.readModels);
    if (declaration === null) return undefined;
    const names = identifierNames(declaration.node, declaration.slice);
    return names.length === 1 ? declaration.node.properties.find(property => property.name === names[0])?.type : undefined;
}

function identifierNames(model: ReadModelSyntax, owner: SliceSyntax): readonly string[] {
    const explicit = model.properties.filter(property => property.isKey).map(property => property.name);
    if (explicit.length > 0) return explicit;
    const queries = owner.queries.filter(query => query.returnType.name.split('.').at(-1) === model.name);
    const identifiers = queryIdentifiers(queries, false);
    if (identifiers.length > 0) return identifiers;
    return queries.some(isAdmittedListQuery) ? conventionalIdentifiers(model, queries) : [];
}

function queryIdentifiers(queries: readonly QuerySyntax[], collection: boolean): readonly string[] {
    return [...new Set(queries.filter(query => query.by !== null)
        .filter(query => query.returnType.isCollection === collection).map(query => query.by!.name))];
}

function isAdmittedListQuery(query: QuerySyntax): boolean {
    return query.returnType.isCollection && query.by?.source == null && query.filters.every(filter => filter.source === null) && query.scope == null;
}

function conventionalIdentifiers(model: ReadModelSyntax, queries: readonly QuerySyntax[]): readonly string[] {
    const collectionKeys = new Set(queryIdentifiers(queries, true));
    const conventional = [...new Set(model.properties.map(property => property.name).filter(name => name.endsWith('Id')))];
    const outsideCollectionKeys = conventional.filter(name => !collectionKeys.has(name));
    return outsideCollectionKeys.length === 1 ? outsideCollectionKeys : conventional;
}
