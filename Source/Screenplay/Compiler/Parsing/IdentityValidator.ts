// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthorizeSyntax, PolicyRequirementSyntax } from '../Syntax/Authorization';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { ExpressionSyntax, IdentityExpressionSyntax } from '../Syntax/Expressions';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { identityProperties } from './ExpressionParser';
import { ParserContext } from './ParserContext';

export function validateIdentity(application: ApplicationSyntax, context: ParserContext): void {
    const details = application.identity?.details ?? [];
    const names = new Set(details.map(detail => detail.name));
    const known = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', ...application.concepts.map(concept => concept.name), ...application.types.map(type => type.name), ...application.imports.map(imported => imported.qualifiedName.split('.').at(-1)!) ]);
    const resolver = new AuthoringProductionResolver(application);
    for (const { slice } of resolver.slices) for (const model of slice.readModels) known.add(model.name);
    new IdentityPaths(names, context).visitApplication(application);
    const declared = new Set<string>();
    for (const detail of details) {
        if (declared.has(detail.name)) context.error(DiagnosticCodes.DuplicateIdentityDetail, `Duplicate identity detail '${detail.name}' - detail names must be unique`, detail.location);
        declared.add(detail.name);
        if (identityProperties.includes(detail.name)) context.error(DiagnosticCodes.BuiltInIdentityDetail, `Identity detail '${detail.name}' redeclares a built-in caller property`, detail.location);
        if (!known.has(detail.type.name)) context.warning(DiagnosticCodes.UnknownType, `Unknown type '${detail.type.name}' on identity detail '${detail.name}'`, detail.type.location);
        if (detail.source.kind !== 'QueryIdentitySourceSyntax') continue;
        const source = detail.source;
        if (!isTokenKey(source.by)) context.error(DiagnosticCodes.InvalidIdentityQueryKey, `Identity detail '${detail.name}' query key may reference only caller built-ins, claims or literals, never another detail`, source.by.location);
        const parts = source.query.split('.');
        const name = parts.at(-1)!;
        const qualifiers = parts.slice(0, -1);
        const queries = resolver.slices.flatMap(({ slice, scope }) => slice.queries.map(query => ({ query, scope }))).filter(entry => entry.query.name === name && qualifiers.length <= entry.scope.length && qualifiers.every((qualifier, index) => qualifier === entry.scope[entry.scope.length - qualifiers.length + index]));
        if (queries.length !== 1) {
            context.error(DiagnosticCodes.UnknownIdentityQuery, `Unknown or ambiguous identity query '${source.query}'`, source.location);
            continue;
        }
        const { query, scope } = queries[0];
        if (query.by === null || query.returnType.isCollection) context.error(DiagnosticCodes.InvalidIdentityQuery, `Identity query '${source.query}' must be keyed and return a single, possibly optional result`, source.location);
        if (query.returnType.name !== detail.type.name || detail.type.isCollection || query.returnType.isOptional && !detail.type.isOptional) context.error(DiagnosticCodes.IdentityQueryTypeMismatch, `Identity detail '${detail.name}' must match query '${source.query}' result type, including optionality`, detail.type.location);
        const authorizations: AuthorizeSyntax[] = query.authorize === null ? [] : [query.authorize];
        for (const module of application.modules.filter(module => module.name === scope[0])) {
            if (module.authorize !== null) authorizations.push(module.authorize);
            collectFeatureAuthorizations(module.features, scope.slice(1), authorizations);
        }
        const policies = new Set(authorizations.flatMap(authorization => policyNames(authorization.requirement)));
        const dependency = new DetailReferences(names);
        for (const policy of (application.policies ?? []).filter(policy => policies.has(policy.name))) dependency.visitPolicy(policy);
        if (dependency.found.length > 0) context.error(DiagnosticCodes.IdentityQueryAuthorizationDependency, `Identity query '${source.query}' authorization depends on identity detail '${dependency.found[0].path.split('.')[0]}' - caller details cannot authorize their own resolution`, source.location);
    }
}

function isTokenKey(expression: ExpressionSyntax): boolean {
    if (expression.kind === 'LiteralExpressionSyntax') return true;
    if (expression.kind === 'IdentityExpressionSyntax') return identityProperties.includes(expression.path.split('.')[0]);
    return expression.kind === 'ContextExpressionSyntax' && expression.path.startsWith('identity.') && identityProperties.includes(expression.path.split('.')[1]);
}

function policyNames(requirement: PolicyRequirementSyntax): string[] {
    if (requirement.kind === 'PolicyReferenceSyntax') return [requirement.name];
    return [...policyNames(requirement.left), ...policyNames(requirement.right)];
}

function collectFeatureAuthorizations(features: readonly FeatureSyntax[], segments: readonly string[], result: AuthorizeSyntax[]): void {
    for (const feature of features.filter(feature => feature.name === segments[0])) {
        if (feature.authorize !== null) result.push(feature.authorize);
        collectFeatureAuthorizations(feature.features, segments.slice(1), result);
    }
}

class IdentityPaths extends ScreenplaySyntaxWalker {
    constructor(private readonly details: ReadonlySet<string>, private readonly context: ParserContext) { super(); }
    override visitExpression(syntax: ExpressionSyntax): void {
        if (syntax.kind === 'ContextExpressionSyntax' && syntax.path.startsWith('identity.')) {
            const property = syntax.path.split('.')[1];
            if (!identityProperties.includes(property) && !this.context.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.UnknownContextIdentityProperty && diagnostic.location.line === syntax.location.line && diagnostic.location.column === syntax.location.column)) {
                this.context.warning(DiagnosticCodes.UnknownContextIdentityProperty, `Unknown $context.identity property '${property}' - expected ${identityProperties.join(', ')}`, syntax.location);
            }
        }
        super.visitExpression(syntax);
    }
    override visitIdentityExpression(syntax: IdentityExpressionSyntax): void {
        const property = syntax.path.split('.')[0];
        if (!this.details.has(property) && !identityProperties.includes(property)) this.context.warning(DiagnosticCodes.UnknownContextIdentityProperty, `Unknown $identity property '${property}' - expected ${[...identityProperties, ...this.details].join(', ')}`, syntax.location);
    }
}

class DetailReferences extends ScreenplaySyntaxWalker {
    readonly found: IdentityExpressionSyntax[] = [];
    constructor(private readonly details: ReadonlySet<string>) { super(); }
    override visitIdentityExpression(syntax: IdentityExpressionSyntax): void {
        if (this.details.has(syntax.path.split('.')[0])) this.found.push(syntax);
    }
}
