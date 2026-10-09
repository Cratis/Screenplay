// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from './Expressions';
import { SpecificationExampleSyntax, SpecificationStreamSyntax, SpecificationNoStreamSyntax, SpecificationSyntax } from './Specifications';
import { SyntaxNode } from './SyntaxNode';
import { ApplicationSyntax } from './Structure';

export type SpecificationValueOrigin = 'authored' | 'example' | 'override' | 'persona' | 'case';

export interface EffectiveSpecificationValue {
    readonly property: string;
    readonly value: ExpressionSyntax;
    readonly origin: SpecificationValueOrigin;
    readonly overriddenValue: ExpressionSyntax | null;
    readonly persona?: string;
    readonly policy?: string;
    readonly caseParameter?: string;
}

export interface EffectiveSpecificationRoute {
    readonly value: SpecificationStreamSyntax | SpecificationNoStreamSyntax;
    readonly origin: SpecificationValueOrigin;
    readonly overriddenValue: SpecificationStreamSyntax | SpecificationNoStreamSyntax | null;
}

export interface EffectiveSpecificationStep {
    readonly role: string;
    readonly authored: SyntaxNode;
    readonly effective: SyntaxNode;
    readonly example: SpecificationExampleSyntax | null;
    readonly values: readonly EffectiveSpecificationValue[];
    readonly route: EffectiveSpecificationRoute | null;
}

export interface EffectiveSpecification {
    readonly authored: SpecificationSyntax;
    readonly table?: string;
    readonly case?: string;
    readonly effective: SpecificationSyntax;
    readonly steps: readonly EffectiveSpecificationStep[];
}

export interface EffectiveSpecificationApplication {
    readonly application: ApplicationSyntax;
    readonly specifications: readonly EffectiveSpecification[];
}
