// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    ApplicationCompilation,
    ApplicationSyntax,
    FeatureSyntax,
    inMemoryDocumentSource,
    isWithinOrSame,
    ModuleSyntax,
    normalizePlayPath,
    parse,
    PlayPlacement,
    resolveImports,
    SliceSyntax,
} from '@cratis/screenplay-compiler';

// What one document of an application stands for on the board: the files it is made of - itself and every file
// its imports bring in, however deep - and, for a document that holds no slice of its own, the modules and
// features it declares. A slice file is its slices; a feature or module file that imports its parts is those
// parts; a module file that only declares the module and its features is everything placed in them.
export interface BoardScope {
    readonly files: ReadonlySet<string>;
    readonly containers: readonly PlayPlacement[];
}

// The scope of a document, found in the application it was compiled as part of.
export function scopeOf(path: string, documents: ReadonlyMap<string, string>, application: ApplicationCompilation): BoardScope {
    const key = normalizePlayPath(path);
    const texts = new Map([...documents].map(([file, text]) => [normalizePlayPath(file), text]));
    const files = new Set(resolveImports([key], inMemoryDocumentSource(texts)).documents.map(document => document.path));
    files.add(key);

    const text = texts.get(key) ?? '';
    const placement = application.documents.find(document => document.path === key)?.placement ?? [];
    const own = parse(text, key, placement).value;
    return { files, containers: slicesOf(own).length > 0 ? [] : declaredContainers(own) };
}

// The application narrowed to the slices in a scope - with the modules and features around them, and everything
// they are written against left as it is, so the slices draw just as they do in the whole application. A scope
// that holds no slice gives undefined: the document contributes nothing a board draws on its own.
export function narrowTo(application: ApplicationSyntax, scope: BoardScope): ApplicationSyntax | undefined {
    const inScope = (slice: SliceSyntax, container: PlayPlacement) =>
        (slice.location.path !== undefined && scope.files.has(normalizePlayPath(slice.location.path))) ||
        scope.containers.some(declared => isWithinOrSame(container, declared));

    const feature = (syntax: FeatureSyntax, container: PlayPlacement): FeatureSyntax | undefined => {
        const path = [...container, syntax.name];
        const slices = syntax.slices.filter(slice => inScope(slice, path));
        const features = syntax.features.map(nested => feature(nested, path)).filter(isDefined);
        return slices.length + features.length === 0 ? undefined : { ...syntax, slices, features };
    };
    const module = (syntax: ModuleSyntax): ModuleSyntax | undefined => {
        const features = syntax.features.map(nested => feature(nested, [syntax.name])).filter(isDefined);
        return features.length === 0 ? undefined : { ...syntax, features };
    };

    const modules = application.modules.map(module).filter(isDefined);
    return modules.length === 0 ? undefined : { ...application, modules };
}

// The files the slices of an application were written in.
export function filesOf(application: ApplicationSyntax): Set<string> {
    return new Set(slicesOf(application).map(slice => slice.location.path).filter(isDefined).map(normalizePlayPath));
}

function slicesOf(application: ApplicationSyntax): SliceSyntax[] {
    const inFeature = (feature: FeatureSyntax): SliceSyntax[] => [...feature.slices, ...feature.features.flatMap(inFeature)];
    return application.modules.flatMap(module => module.features.flatMap(inFeature));
}

function declaredContainers(application: ApplicationSyntax): PlayPlacement[] {
    const containers: PlayPlacement[] = [];
    const visit = (feature: FeatureSyntax, container: PlayPlacement) => {
        const path = [...container, feature.name];
        if (!feature.isPlacement) containers.push(path);
        feature.features.forEach(nested => visit(nested, path));
    };
    for (const module of application.modules) {
        if (!module.isPlacement) containers.push([module.name]);
        module.features.forEach(feature => visit(feature, [module.name]));
    }
    return containers;
}

function isDefined<T>(value: T | undefined): value is T {
    return value !== undefined;
}
