// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizeSyntax, PersonaSyntax } from './Authorization';
import { CaptureSyntax } from './Captures';
import { CommandSyntax } from './Commands';
import { ConstraintSyntax } from './Constraints';
import { ConceptSyntax, DomainSyntax, EventSyntax, ImportSyntax, ReadModelSyntax, TypeSyntax } from './Declarations';
import { ProjectionSyntax } from './Projections';
import { OperationSyntax, SystemSyntax } from './Operations';
import { QuerySyntax } from './Queries';
import { ReactionSyntax } from './Reactions';
import { ScreenSyntax } from './Screens';
import { SpecificationSyntax } from './Specifications';
import { SyntaxNode } from './SyntaxNode';

// The four kinds of slice. The names are the C# SliceType members, which is also how SyntaxJson writes them.
export type SliceType = 'StateChange' | 'StateView' | 'Automation' | 'Translate';

export const sliceTypes: readonly SliceType[] = ['StateChange', 'StateView', 'Automation', 'Translate'];

export interface SliceSyntax extends SyntaxNode {
    readonly kind: 'SliceSyntax';
    readonly type: SliceType;
    readonly name: string;
    readonly description: string | null;
    readonly events: readonly EventSyntax[];
    readonly operations?: readonly OperationSyntax[];
    readonly commands: readonly CommandSyntax[];
    readonly queries: readonly QuerySyntax[];
    readonly projections: readonly ProjectionSyntax[];
    readonly captures: readonly CaptureSyntax[];
    readonly reactions: readonly ReactionSyntax[];
    readonly constraints: readonly ConstraintSyntax[];
    readonly specifications: readonly SpecificationSyntax[];
    readonly readModels: readonly ReadModelSyntax[];
    readonly screens: readonly ScreenSyntax[];
}

// An 'import "<pattern>"' of other .play files. Where it is written decides where what it imports belongs:
// at the top level of a document it brings in whole documents; inside a module or feature it places each
// imported file there, so the file's top level is that module's or feature's body.
export interface FileImportSyntax extends SyntaxNode {
    readonly kind: 'FileImportSyntax';
    readonly pattern: string;
}

export interface FeatureSyntax extends SyntaxNode {
    readonly kind: 'FeatureSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly authorize: AuthorizeSyntax | null;
    readonly features: readonly FeatureSyntax[];
    readonly slices: readonly SliceSyntax[];
    readonly fileImports: readonly FileImportSyntax[];

    // Whether the feature is not written in the document but places it - the document was imported into
    // the feature, so its top level is the feature's body.
    readonly isPlacement: boolean;
}

export interface ModuleSyntax extends SyntaxNode {
    readonly kind: 'ModuleSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly authorize: AuthorizeSyntax | null;
    readonly features: readonly FeatureSyntax[];
    readonly fileImports: readonly FileImportSyntax[];

    // Whether the module is not written in the document but places it - the document was imported into
    // the module, so its top level is the module's body.
    readonly isPlacement: boolean;
}

export interface ApplicationSyntax extends SyntaxNode {
    readonly kind: 'ApplicationSyntax';
    readonly domain: DomainSyntax | null;
    readonly systems?: readonly SystemSyntax[];
    readonly imports: readonly ImportSyntax[];
    readonly concepts: readonly ConceptSyntax[];
    readonly types: readonly TypeSyntax[];
    readonly modules: readonly ModuleSyntax[];
    readonly personas: readonly PersonaSyntax[];

    // The files the document imports at its top level - whole documents, merged into the application.
    readonly fileImports: readonly FileImportSyntax[];
}
