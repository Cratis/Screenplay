// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { eventDeclarations } from './EventDeclarations';
import { EventSyntax } from './Declarations';
import { operationDeclarations } from './Operations';
import { ProducesSyntax } from './Reactions';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from './Structure';

import { AuthoringProductionKind } from './AuthoringProductionKind';
import { AuthoringProductionDeclaration } from './AuthoringProductionDeclaration';
import { AuthoringProductionResolution } from './AuthoringProductionResolution';

export { AuthoringProductionKind } from './AuthoringProductionKind';
export type { AuthoringProductionDeclaration } from './AuthoringProductionDeclaration';
export type { AuthoringProductionResolution } from './AuthoringProductionResolution';

export class AuthoringProductionResolver {
    readonly declarations: AuthoringProductionDeclaration[] = [];
    readonly slices: { slice: SliceSyntax; scope: readonly string[] }[] = [];
    private readonly byName = new Map<string, AuthoringProductionDeclaration[]>();
    private readonly scopes = new Map<SliceSyntax, readonly string[]>();
    private readonly resolutions = new Map<SliceSyntax, Map<string, AuthoringProductionResolution>>();

    constructor(application: ApplicationSyntax) {
        const collect = (features: readonly FeatureSyntax[], parent: readonly string[]): void => {
            for (const feature of features) {
                const scope = [...parent, feature.name];
                for (const slice of feature.slices) this.slices.push({ slice, scope: [...scope, slice.name] });
                collect(feature.features, scope);
            }
        };
        application.modules.forEach(module => collect(module.features, [module.name]));
        for (const { slice, scope } of this.slices) {
            this.scopes.set(slice, scope);
            const events = new Map<string, EventSyntax>();
            for (const event of eventDeclarations(slice)) {
                if ((events.get(event.name)?.generation ?? 0) <= event.generation) events.set(event.name, event);
            }
            this.declarations.push(...[...events.values()].map(node => ({ kind: AuthoringProductionKind.Event as const, name: node.name, node, scope })),
                ...operationDeclarations(slice).map(node => ({ kind: AuthoringProductionKind.Operation as const, name: node.name, node, scope })));
        }
        for (const entry of this.declarations) {
            const named = this.byName.get(entry.name) ?? [];
            named.push(entry);
            this.byName.set(entry.name, named);
        }
    }

    resolve(reference: string, slice: SliceSyntax): AuthoringProductionResolution {
        const scope = this.scopes.get(slice);
        if (scope === undefined) return { kind: AuthoringProductionKind.Unresolved, declaration: null, candidates: [] };
        const cache = this.resolutions.get(slice) ?? new Map<string, AuthoringProductionResolution>();
        const cached = cache.get(reference);
        if (cached !== undefined) return cached;
        const candidates = this.candidates(reference, scope);
        const resolution = candidates.length === 1
            ? { kind: candidates[0].kind, declaration: candidates[0], candidates: [] }
            : { kind: candidates.length === 0 ? AuthoringProductionKind.Unresolved : AuthoringProductionKind.Ambiguous, declaration: null, candidates };
        cache.set(reference, resolution);
        this.resolutions.set(slice, cache);
        return resolution;
    }

    isOperation(production: ProducesSyntax, slice: SliceSyntax): boolean {
        return production.inlineOperation != null || this.resolve(production.event, slice).kind === AuthoringProductionKind.Operation;
    }

    isEventProduction(production: ProducesSyntax, slice: SliceSyntax): boolean {
        if (production.inlineOperation != null) return false;
        const resolution = this.resolve(production.event, slice);
        return resolution.kind !== AuthoringProductionKind.Operation && !(resolution.kind === AuthoringProductionKind.Ambiguous && resolution.candidates.some(candidate => candidate.kind === AuthoringProductionKind.Operation));
    }

    private candidates(reference: string, from: readonly string[]): AuthoringProductionDeclaration[] {
        const segments = reference.split('.');
        const named = this.byName.get(segments.at(-1)!) ?? [];
        const qualifiers = segments.slice(0, -1);
        if (qualifiers.length > 0) return named.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((value, index) => entry.scope[entry.scope.length - qualifiers.length + index] === value));
        for (let depth = from.length; depth >= 0; depth--) {
            const visible = named.filter(entry => depth <= entry.scope.length && from.slice(0, depth).every((value, index) => entry.scope[index] === value));
            if (visible.length > 0) return visible;
        }
        return [];
    }
}
