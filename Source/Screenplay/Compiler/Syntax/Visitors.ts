// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConstraintSyntax } from './Constraints';
import { SpecificationSyntax } from './Specifications';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax, SliceSyntax } from './Structure';

// The hooks a consumer turns a syntax tree into its own artifacts with - the same shape as the C#
// IApplicationSyntaxVisitor<T> and its siblings, so an emitter reads the same in either language.

export interface ApplicationSyntaxVisitor<TApplication> {
    visit(syntax: ApplicationSyntax): TApplication;
}

export interface ModuleSyntaxVisitor<TModule> {
    visit(syntax: ModuleSyntax): TModule;
}

export interface FeatureSyntaxVisitor<TFeature> {
    visit(syntax: FeatureSyntax): TFeature;
}

export interface SliceSyntaxVisitor<TSlice> {
    visit(syntax: SliceSyntax): TSlice;
}

export interface ConstraintSyntaxVisitor<TConstraint> {
    visit(syntax: ConstraintSyntax): TConstraint;
}

export interface SpecificationSyntaxVisitor<TSpecification> {
    visit(syntax: SpecificationSyntax): TSpecification;
}
