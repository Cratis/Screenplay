// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertySyntax, TypeSyntax } from '../Syntax/Declarations';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { uniqueByName } from './ResponseValidator';

/** A property a path resolved to, or why it did not resolve. */
export interface ResolvedProperty {
    /** The property the path names, or null when it is absent or its shape is unknown. */
    readonly value: PropertySyntax | null;
    /** Whether the path is provably absent rather than unknown. */
    readonly missing: boolean;
}

/** The shapes projection validators resolve read-model, element and event properties against. */
export interface ProjectionShapes {
    /** The composite types, by name. */
    readonly types: Map<string, TypeSyntax>;
    /** Resolves a dotted property path within a set of properties, following composite types. */
    property(properties: readonly PropertySyntax[] | null, path: string): ResolvedProperty;
    /** Resolves the properties of the read model a name refers to from a scope, or null when the shape is unknown. */
    viewProperties(reference: string, scope: readonly string[]): readonly PropertySyntax[] | null;
}

/**
 * Builds the shape lookups projection validators share.
 * @param application The application whose declarations are resolved.
 * @param slices The scoped slices of the application.
 * @returns The shape lookups.
 */
export function projectionShapes(application: ApplicationSyntax, slices: readonly { slice: SliceSyntax; scope: readonly string[] }[]): ProjectionShapes {
    const types = uniqueByName(application.types);
    const scalar = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', ...application.concepts.map(concept => concept.name)].filter(name => !types.has(name)));
    const property = (properties: readonly PropertySyntax[] | null, path: string): ResolvedProperty => {
        const [name, ...rest] = path.split('.');
        if (properties === null) return { value: null, missing: false };
        const matches = properties.filter(entry => entry.name === name);
        if (matches.length !== 1) return { value: null, missing: matches.length === 0 };
        const value = matches[0];
        if (rest.length === 0) return { value, missing: false };
        if (scalar.has(value.type.name)) return { value: null, missing: true };
        return property(types.get(value.type.name)?.properties ?? null, rest.join('.'));
    };
    const views = slices.flatMap(({ slice, scope }) => slice.readModels.map(node => ({ node, scope })));
    const viewProperties = (reference: string, scope: readonly string[]): readonly PropertySyntax[] | null => {
        const candidates = (name: string) => {
            const segments = name.split('.');
            const named = views.filter(entry => entry.node.name === segments.at(-1));
            const qualifiers = segments.slice(0, -1);
            if (qualifiers.length > 0) return named.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((value, index) => entry.scope[entry.scope.length - qualifiers.length + index] === value));
            for (let depth = scope.length; depth >= 0; depth--) {
                const visible = named.filter(entry => depth <= entry.scope.length && scope.slice(0, depth).every((value, index) => entry.scope[index] === value));
                if (visible.length > 0) return visible;
            }
            return [];
        };
        let matches = candidates(reference);
        const imports = application.imports.filter(entry => entry.qualifiedName.split('.').at(-1) === reference);
        if (matches.length === 0 && imports.length === 1) matches = candidates(imports[0].qualifiedName);
        return matches.length === 1 ? matches[0].node.properties : null;
    };
    return { types, property, viewProperties };
}
