// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { combineAuthorize } from '../Parsing/AuthorizeParser';
import { CompilationResult } from '../ScreenplayCompiler';
import { AuthorizeSyntax, PersonaSyntax } from '../Syntax/Authorization';
import { ConceptSyntax, TypeSyntax } from '../Syntax/Declarations';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax } from '../Syntax/Structure';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { invalidSourceOptions, isAuthoredDocument, legacySourceOptions, SourceOptions, validatedSourceOptions } from '../Syntax/SourceOptions';
import { InvalidSyntaxJson } from '../Syntax/InvalidSyntaxJson';

// "The documents of a folder are one document": modules and features with the same name combine, and a
// name declared in two files is reported - the port of the C# PlayFolderMerge for what this compiler
// models. The semantic validation the C# merge runs afterwards is not ported.
export function mergeDocuments(documents: readonly CompilationResult<ApplicationSyntax>[]): CompilationResult<ApplicationSyntax> {
    const diagnostics: Diagnostic[] = [];
    const applications = documents.map(document => document.value);
    const options = new Map<ApplicationSyntax, SourceOptions>();
    let invalid = false;
    for (const application of applications) {
        try {
            options.set(application, Object.hasOwn(application, 'sourceOptions') ? validatedSourceOptions(application.sourceOptions) : legacySourceOptions);
        } catch (failure) {
            if (!(failure instanceof InvalidSyntaxJson)) throw failure;
            invalid = true;
            diagnostics.push(error(DiagnosticCodes.IncompatibleNumericSource, failure.message, application.location));
        }
    }
    const asserted = applications.filter(application => options.get(application)?.numericMode === 'exact' || hasDeclarations(application));
    let sourceOptions = options.get(asserted[0]) ?? legacySourceOptions;
    for (const application of asserted.slice(1)) {
        if (options.get(application)?.numericMode !== sourceOptions.numericMode) {
            invalid = true;
            diagnostics.push(error(DiagnosticCodes.MixedNumericModes, 'Declaration-bearing documents and marked import barrels must independently select the same numeric mode.', application.location));
        }
    }
    if (invalid) sourceOptions = invalidSourceOptions;
    const named = new Map<string, SourceLocation>();
    const concepts = declaredInOneFile<ConceptSyntax>(applications.flatMap(application => application.concepts), 'declaration of', diagnostics, undefined, named);
    const types = declaredInOneFile<TypeSyntax>(applications.flatMap(application => application.types), 'declaration of', diagnostics, undefined, named);
    const domains = applications.map(application => application.domain).filter(domain => domain !== null);
    for (const extra of domains.slice(1)) {
        diagnostics.push(error(DiagnosticCodes.RepeatedSingularDeclarationAcrossFiles,
            `The folder already declares a domain in '${describe(domains[0].location.path)}' - a folder compiles to one application, which can have at most one`, extra.location));
    }
    // Modules merge before personas are claimed, so the diagnostics come in the order the C# merge reports them.
    const modules = mergeModules(applications.flatMap(application => application.modules), diagnostics);
    const personas = declaredInOneFile<PersonaSyntax>(applications.flatMap(application => application.personas), 'persona', diagnostics);
    const value: ApplicationSyntax = {
        kind: 'ApplicationSyntax',
        sourceOptions,
        domain: domains[0] ?? null,
        imports: firstOfEach(applications.flatMap(application => application.imports), item => item.qualifiedName),
        concepts,
        types,
        examples: applications.flatMap(application => application.examples ?? []),
        systems: applications.flatMap(application => application.systems ?? []),
        eventSources: applications.flatMap(application => application.eventSources ?? []),
        declaredTriggers: applications.flatMap(application => application.declaredTriggers ?? []),
        modules,
        personas,
        policies: applications.flatMap(application => application.policies ?? []),
        seeds: applications.flatMap(application => application.seeds ?? []),
        fileImports: applications.flatMap(application => application.fileImports),
        location: applications[0]?.location ?? { line: 1, column: 1 },
    };
    const all = [...documents.flatMap(document => document.diagnostics), ...diagnostics];
    return { value, diagnostics: all, success: !all.some(diagnostic => diagnostic.severity === 'error') };
}

function hasDeclarations(application: ApplicationSyntax): boolean {
    if (isAuthoredDocument(application)) return true;
    if (application.domain !== null || application.concepts.length > 0 || application.types.length > 0 || application.personas.length > 0 || (application.policies?.length ?? 0) > 0 || (application.seeds?.length ?? 0) > 0 || (application.systems?.length ?? 0) > 0 || (application.eventSources?.length ?? 0) > 0 || (application.examples?.length ?? 0) > 0) return true;
    const feature = (node: FeatureSyntax): boolean => !node.isPlacement || (node.examples?.length ?? 0) > 0 || node.slices.length > 0 || node.features.some(feature);
    return application.modules.some(module => !module.isPlacement || (module.examples?.length ?? 0) > 0 || module.features.some(feature));
}

function mergeModules(modules: readonly ModuleSyntax[], diagnostics: Diagnostic[]): ModuleSyntax[] {
    return groupByName(modules).map(placedLast).map(parts => parts.length === 1 ? { ...parts[0], isPlacement: false, features: parts[0].features.map(unplaced) } : {
        ...parts[0],
        isPlacement: false,
        fileImports: parts.flatMap(part => part.fileImports),
        examples: parts.flatMap(part => part.examples ?? []),
        description: firstDescription(parts, `module '${parts[0].name}'`, diagnostics),
        authorize: combineAuthorization(parts.map(part => part.authorize), `module '${parts[0].name}'`, diagnostics),
        features: mergeFeatures(parts.flatMap(part => part.features), diagnostics),
    });
}

function mergeFeatures(features: readonly FeatureSyntax[], diagnostics: Diagnostic[]): FeatureSyntax[] {
    return groupByName(features).map(placedLast).map(parts => parts.length === 1 ? unplaced(parts[0]) : {
        ...parts[0],
        isPlacement: false,
        fileImports: parts.flatMap(part => part.fileImports),
        examples: parts.flatMap(part => part.examples ?? []),
        description: firstDescription(parts, `feature '${parts[0].name}'`, diagnostics),
        authorize: combineAuthorization(parts.map(part => part.authorize), `feature '${parts[0].name}'`, diagnostics),
        features: mergeFeatures(parts.flatMap(part => part.features), diagnostics),
        slices: declaredInOneFile(parts.flatMap(part => part.slices), 'slice', diagnostics, `feature '${parts[0].name}'`),
    });
}

// Where a module or feature is written comes before files merely placed in it, so the merged one is located
// at its declaration. Within each kind, path order is kept.
function placedLast<T extends { readonly isPlacement: boolean }>(parts: readonly T[]): T[] {
    return [...parts.filter(part => !part.isPlacement), ...parts.filter(part => part.isPlacement)];
}

// Once merged, a feature is the feature - whether a file was placed in it no longer says anything.
function unplaced(feature: FeatureSyntax): FeatureSyntax {
    return { ...feature, isPlacement: false, features: feature.features.map(unplaced) };
}

// Combines the gates files declare on one module or feature with 'and', never weakening an earlier one. The
// same gate repeated in another file says nothing new and is reported and left out.
function combineAuthorization(declarations: readonly (AuthorizeSyntax | null)[], owner: string, diagnostics: Diagnostic[]): AuthorizeSyntax | null {
    const kept: AuthorizeSyntax[] = [];
    for (const authorization of declarations.filter(declaration => declaration !== null)) {
        const first = kept.find(earlier => earlier.location.path !== authorization.location.path
            && JSON.stringify(toSyntaxJson(earlier)) === JSON.stringify(toSyntaxJson(authorization)));
        if (first !== undefined) {
            diagnostics.push(warning(DiagnosticCodes.DuplicateAuthorizationAcrossFiles,
                `An identical authorization is already declared on the ${owner} in '${describe(first.location.path)}' - this repeated gate is ignored`, authorization.location));
            continue;
        }
        kept.push(authorization);
    }
    return kept.reduce<AuthorizeSyntax | null>((combined, next) => combineAuthorize(combined, next), null);
}

function firstOfEach<T>(items: readonly T[], key: (item: T) => string): T[] {
    const seen = new Set<string>();
    return items.filter(item => !seen.has(key(item)) && seen.add(key(item)) !== undefined);
}

// Groups by name, keeping the order each name first appears in.
function groupByName<T extends { readonly name: string }>(items: readonly T[]): T[][] {
    const groups = new Map<string, T[]>();
    for (const item of items) {
        groups.set(item.name, [...(groups.get(item.name) ?? []), item]);
    }
    return [...groups.values()];
}

function firstDescription(parts: readonly { description: string | null; location: SourceLocation }[], owner: string, diagnostics: Diagnostic[]): string | null {
    const described = parts.filter(part => part.description !== null);
    for (const disagreeing of described.slice(1).filter(part => part.description !== described[0].description)) {
        diagnostics.push(warning(DiagnosticCodes.ConflictingDescriptionAcrossFiles,
            `The ${owner} is already described in '${describe(described[0].location.path)}' - keeping that description`, disagreeing.location));
    }
    return described[0]?.description ?? null;
}

// Keeps the first declaration of each name; one in another file with the same name is reported and dropped.
// A repeat within one file is left for that file's own checks.
function declaredInOneFile<T extends { readonly name: string; readonly location: SourceLocation }>(
    declarations: readonly T[], keyword: string, diagnostics: Diagnostic[], within?: string, claimed = new Map<string, SourceLocation>()): T[] {
    const kept: T[] = [];
    for (const declaration of declarations) {
        const first = claimed.get(declaration.name);
        if (first !== undefined && first.path !== declaration.location.path) {
            diagnostics.push(error(DiagnosticCodes.RepeatedDeclarationAcrossFiles,
                `Duplicate ${keyword} '${declaration.name}'${within === undefined ? '' : ` in ${within}`} - already declared in '${describe(first.path)}'`, declaration.location));
            continue;
        }
        if (first === undefined) {
            claimed.set(declaration.name, declaration.location);
        }
        kept.push(declaration);
    }
    return kept;
}

const describe = (path: string | undefined): string => path ?? 'another file';
const error = (code: string, message: string, location: SourceLocation): Diagnostic => ({ severity: 'error', code, message, location });
const warning = (code: string, message: string, location: SourceLocation): Diagnostic => ({ severity: 'warning', code, message, location });
