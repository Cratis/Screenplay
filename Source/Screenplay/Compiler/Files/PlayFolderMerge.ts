// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ConceptSyntax, TypeSyntax } from '../Syntax/Declarations';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax } from '../Syntax/Structure';

// One .play document of a folder: its path relative to the folder, and its text.
export interface PlayFileSource {
    readonly path: string;
    readonly source: string;
}

// Compiles the documents of a folder as one application - the counterpart of the C# CompileFolder. The
// documents are read in ordinal order of their relative paths, as the C# compiler reads them, so a merge
// keeps the same first declaration in either language.
export function parseFolder(files: readonly PlayFileSource[]): CompilationResult<ApplicationSyntax> {
    const ordered = [...files].sort((left, right) => ordinal(left.path, right.path));
    return mergeDocuments(ordered.map(file => parse(file.source, file.path)));
}

// "The documents of a folder are one document": modules and features with the same name combine, and a
// name declared in two files is reported - the port of the C# PlayFolderMerge for what this compiler
// models. The semantic validation the C# merge runs afterwards is not ported.
export function mergeDocuments(documents: readonly CompilationResult<ApplicationSyntax>[]): CompilationResult<ApplicationSyntax> {
    const diagnostics: Diagnostic[] = [];
    const applications = documents.map(document => document.value);
    const named = new Map<string, SourceLocation>();
    const concepts = declaredInOneFile<ConceptSyntax>(applications.flatMap(application => application.concepts), 'declaration of', diagnostics, undefined, named);
    const types = declaredInOneFile<TypeSyntax>(applications.flatMap(application => application.types), 'declaration of', diagnostics, undefined, named);
    const domains = applications.map(application => application.domain).filter(domain => domain !== null);
    for (const extra of domains.slice(1)) {
        diagnostics.push(error(DiagnosticCodes.RepeatedSingularDeclarationAcrossFiles,
            `The folder already declares a domain in '${describe(domains[0].location.path)}' - a folder compiles to one application, which can have at most one`, extra.location));
    }
    const value: ApplicationSyntax = {
        kind: 'ApplicationSyntax',
        domain: domains[0] ?? null,
        imports: firstOfEach(applications.flatMap(application => application.imports), item => item.qualifiedName),
        concepts,
        types,
        modules: mergeModules(applications.flatMap(application => application.modules), diagnostics),
        location: applications[0]?.location ?? { line: 1, column: 1 },
    };
    const all = [...documents.flatMap(document => document.diagnostics), ...diagnostics];
    return { value, diagnostics: all, success: !all.some(diagnostic => diagnostic.severity === 'error') };
}

function mergeModules(modules: readonly ModuleSyntax[], diagnostics: Diagnostic[]): ModuleSyntax[] {
    return groupByName(modules).map(parts => parts.length === 1 ? parts[0] : {
        ...parts[0],
        description: firstDescription(parts, `module '${parts[0].name}'`, diagnostics),
        features: mergeFeatures(parts.flatMap(part => part.features), diagnostics),
    });
}

function mergeFeatures(features: readonly FeatureSyntax[], diagnostics: Diagnostic[]): FeatureSyntax[] {
    return groupByName(features).map(parts => parts.length === 1 ? parts[0] : {
        ...parts[0],
        description: firstDescription(parts, `feature '${parts[0].name}'`, diagnostics),
        features: mergeFeatures(parts.flatMap(part => part.features), diagnostics),
        slices: declaredInOneFile(parts.flatMap(part => part.slices), 'slice', diagnostics, `feature '${parts[0].name}'`),
    });
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
const ordinal = (left: string, right: string): number => left < right ? -1 : left > right ? 1 : 0;
