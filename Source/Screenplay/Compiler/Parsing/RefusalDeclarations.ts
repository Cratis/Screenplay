// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { uniqueByName } from './ResponseValidator';

const primitives = new Set(['String', 'Uuid', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime']);

// The scoped declaration facts used by the C# refusal and redelivery consistency validators.
export class RefusalDeclarations {
    readonly slices: AuthoringProductionResolver['slices'];
    readonly types;
    readonly concepts;
    private readonly events;
    private readonly knownTypes: Set<string>;

    constructor(readonly application: ApplicationSyntax) {
        this.slices = new AuthoringProductionResolver(application).slices;
        this.types = uniqueByName(application.types);
        this.concepts = uniqueByName(application.concepts);
        this.knownTypes = new Set([...primitives, ...application.types.map(type => type.name), ...application.concepts.map(concept => concept.name)]);
        this.events = new Map(this.slices.map(({ slice }) => {
            const latest = new Map<string, ReturnType<typeof eventDeclarations>[number]>();
            for (const event of eventDeclarations(slice)) {
                if (!latest.has(event.name) || latest.get(event.name)!.generation < event.generation) latest.set(event.name, event);
            }
            return [slice, [...latest.values()]];
        }));
    }

    resolve<T extends { readonly name: string }>(name: string, from: SliceSyntax, select: (slice: SliceSyntax) => readonly T[]): { node: T; slice: SliceSyntax } | null {
        const entries = this.slices.flatMap(({ slice, scope }) => select(slice).map(node => ({ node, slice, scope })));
        const scope = this.slices.find(entry => entry.slice === from)?.scope ?? [];
        const candidates = (reference: string) => {
            const parts = reference.split('.');
            const named = entries.filter(entry => entry.node.name === parts.at(-1));
            const qualifiers = parts.slice(0, -1);
            if (qualifiers.length > 0) return named.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((part, index) => entry.scope[entry.scope.length - qualifiers.length + index] === part));
            for (let depth = scope.length; depth >= 0; depth--) {
                const visible = named.filter(entry => depth <= entry.scope.length && scope.slice(0, depth).every((part, index) => entry.scope[index] === part));
                if (visible.length > 0) return visible;
            }
            return [];
        };
        let matches = candidates(name);
        if (matches.length === 0) {
            const imports = this.application.imports.filter(imported => imported.qualifiedName.split('.').at(-1) === name);
            if (imports.length === 1) matches = candidates(imports[0].qualifiedName);
        }
        return matches.length === 1 ? matches[0] : null;
    }

    event(name: string, from: SliceSyntax) {
        return this.resolve(name, from, slice => this.events.get(slice) ?? [])?.node ?? null;
    }

    property(properties: readonly PropertySyntax[] | null, path: string): PropertySyntax | null {
        const parts = path.split('.');
        for (const [index, part] of parts.entries()) {
            const matches: readonly PropertySyntax[] = properties?.filter(property => property.name === part) ?? [];
            if (matches.length !== 1) return null;
            const property = matches[0];
            if (index === parts.length - 1) return property;
            if (primitives.has(property.type.name) || this.concepts.has(property.type.name) && !this.application.types.some(type => type.name === property.type.name)) return null;
            properties = this.types.get(property.type.name)?.properties ?? null;
        }
        return null;
    }

    compatible(source: TypeRefSyntax, target: TypeRefSyntax): boolean | null {
        return this.knownTypes.has(source.name) && this.knownTypes.has(target.name)
            ? source.name === target.name && source.isCollection === target.isCollection && (!source.isOptional || target.isOptional) : null;
    }
}
