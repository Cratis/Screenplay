// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { placementFrom } from '../Parsing/ImportDiscovery';
import { discoverImports, parseForAuthoring } from '../ScreenplayCompiler';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { PlacedPlayDocument } from './PlayDocumentSource';
import { matchesPlayPattern, normalizePlayPath, resolvePlayPattern } from './PlayGlob';
import { samePlacement } from './PlayPlacement';
import { AuthoredDeclaration } from './AuthoredDeclaration';

// Presentation metadata never enters syntax JSON, semantic bytes or identity. Merging and diagnostics
// still use the original document order; only a consumer explicitly requesting authored order sees it.
const orders = new WeakMap<ApplicationSyntax, ReadonlyMap<string, number>>();
export const authoredOrderKey = (scope: readonly string[]): string => JSON.stringify(scope);

export function authoredOrderOf(application: ApplicationSyntax): ReadonlyMap<string, number> {
    return orders.get(application) ?? new Map();
}

// A board may narrow an application without changing the order of its remaining declarations.
export function copyAuthoredOrder(original: ApplicationSyntax, narrowed: ApplicationSyntax): ApplicationSyntax {
    const order = orders.get(original);
    if (order !== undefined) orders.set(narrowed, order);
    return narrowed;
}

function declarations(application: ApplicationSyntax): AuthoredDeclaration[] {
    const found: AuthoredDeclaration[] = [];
    const feature = (syntax: FeatureSyntax, outer: readonly string[]) => {
        const scope = [...outer, syntax.name];
        found.push({ scope, ...syntax.location, implicit: syntax.isPlacement, isContainer: true });
        syntax.features.forEach(child => feature(child, scope));
        syntax.slices.forEach(slice => found.push({ scope: [...scope, slice.name], ...slice.location, implicit: false, isContainer: false }));
    };
    application.modules.forEach(module => {
        found.push({ scope: [module.name], ...module.location, implicit: module.isPlacement, isContainer: true });
        module.features.forEach(child => feature(child, [module.name]));
    });
    return found;
}

// A root's explicit declarations, or those in an enclosing container's own named file, define container
// order before globbed slice files that merely restate the same containers. Prefer the outer named
// composite, then the shallower path; absent one, the text walk keeps the first explicit occurrence.
function containerOwners(own: ReadonlyMap<string, readonly AuthoredDeclaration[]>, roots: ReadonlySet<string>): ReadonlyMap<string, string> {
    const owners = new Map<string, { path: string; priority: number; depth: number }>();
    for (const [path, declarations] of own) {
        const segments = path.split('/');
        const name = segments[segments.length - 1].slice(0, -5);
        for (const declaration of declarations.filter(declaration => declaration.isContainer && !declaration.implicit)) {
            const ancestor = declaration.scope.indexOf(name);
            if (!roots.has(path) && ancestor === -1) continue;
            const priority = roots.has(path) ? 0 : ancestor + 1;
            const key = authoredOrderKey(declaration.scope);
            const current = owners.get(key);
            if (current === undefined || priority < current.priority || (priority === current.priority && segments.length < current.depth)) {
                owners.set(key, { path, priority, depth: segments.length });
            }
        }
    }
    return new Map([...owners].map(([key, owner]) => [key, owner.path]));
}

// Walk text, not the merged tree: placement stubs must not claim the position of a declaration that is
// written elsewhere. Expand an import only if it contributes the file's settled (deepest) placement.
export function recordAuthoredOrder(application: ApplicationSyntax, roots: readonly string[], documents: readonly PlacedPlayDocument[], languages?: ReadonlySet<string>): void {
    const files = new Map(documents.map(document => [document.path, document]));
    const own = new Map(documents.filter(document => document.isPlacementResolved !== false).map(document => [document.path, declarations(parseForAuthoring(document.source, document.path, document.placement, false, undefined, languages).value)]));
    const owners = containerOwners(own, new Set(roots.map(normalizePlayPath)));
    const explicit = new Set([...own.values()].flat().filter(declaration => !declaration.implicit).map(declaration => authoredOrderKey(declaration.scope)));
    const order = new Map<string, number>();
    const visited = new Set<string>();
    const visit = (path: string) => {
        const document = files.get(path);
        if (document === undefined || document.isPlacementResolved === false || visited.has(path)) return;
        visited.add(path);
        const entries = [
            ...(own.get(path) ?? []).filter(declaration => {
                const key = authoredOrderKey(declaration.scope);
                return (!declaration.implicit || !explicit.has(key)) && (!declaration.isContainer || !owners.has(key) || owners.get(key) === path);
            })
                .map(declaration => ({ line: declaration.line, column: declaration.column, run: () => {
                    const key = authoredOrderKey(declaration.scope);
                    if (!order.has(key)) order.set(key, order.size);
                } })),
            ...discoverImports(document.source, path, languages).map(imported => ({
                ...imported.fileImport.location,
                run: () => {
                    const placement = placementFrom(imported, document.placement);
                    if (placement === undefined) return;
                    const pattern = resolvePlayPattern(path, imported.fileImport.pattern);
                    [...files.keys()].filter(target => target !== path && matchesPlayPattern(pattern, target)).sort().forEach(target => {
                        if (samePlacement(files.get(target)!.placement, placement)) visit(target);
                    });
                },
            })),
        ];
        entries.sort((left, right) => left.line - right.line || left.column - right.column).forEach(entry => entry.run());
    };
    roots.map(normalizePlayPath).forEach(path => {
        const document = files.get(path);
        // A broad root glob can discover a file before the import that actually places it.
        if (document?.placement.length === 0) visit(path);
    });
    orders.set(application, order);
}
