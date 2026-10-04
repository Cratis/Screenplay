// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnalysisLocation } from './AnalysisLocation';
import { AnalysisType } from './AnalysisType';

// Structural authoring views: no private compiler types or executable identities in the package API.
export interface OperationInput {
    readonly name: string;
    readonly type: AnalysisType;
    readonly location: AnalysisLocation;
}

export interface OperationPhase {
    readonly description: string | null;
    readonly location: AnalysisLocation;
    readonly file: { readonly path: string; readonly location: AnalysisLocation } | null;
    readonly code: { readonly language: string; readonly location: AnalysisLocation } | null;
    readonly implementation: { readonly location: AnalysisLocation; readonly hints: readonly { readonly text: string; readonly location: AnalysisLocation }[] } | null;
}

export interface OperationDeclaration {
    readonly name: string;
    readonly scope: readonly string[];
    readonly location: AnalysisLocation;
    readonly uses: string;
    readonly usesLocation?: AnalysisLocation;
    readonly inputs: readonly OperationInput[];
    readonly description: string | null;
    readonly execute: OperationPhase | null;
    readonly compensate: OperationPhase | null;
}

export interface SystemDeclaration {
    readonly name: string;
    readonly description: string | null;
    readonly location: AnalysisLocation;
}

export interface OperationReference {
    readonly name: string;
    readonly location: AnalysisLocation;
    readonly targetLocation?: AnalysisLocation;
    readonly kind: string;
    readonly declaration: OperationDeclaration | null;
    readonly mappings: readonly { readonly property: string; readonly location: AnalysisLocation; readonly source: { readonly location: AnalysisLocation } }[];
}

export interface OperationAnalysis {
    readonly declarations: readonly OperationDeclaration[];
    readonly systems: readonly SystemDeclaration[];
    readonly references: readonly OperationReference[];
    readonly types: readonly { readonly name: string; readonly properties: readonly OperationInput[] }[];
    readonly concepts: readonly { readonly name: string; readonly type: string; readonly values: readonly string[] }[];
    readonly contexts: ReadonlyMap<number, { readonly scope: readonly string[]; readonly commandLine?: number; readonly operation?: OperationDeclaration; readonly production?: OperationReference }>;
    readonly targets: (scope: readonly string[]) => readonly { readonly name: string; readonly declaration: OperationDeclaration }[];
}
