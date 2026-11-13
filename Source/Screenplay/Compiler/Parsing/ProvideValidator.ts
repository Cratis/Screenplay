// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AvailabilityRequirementSyntax, AvailabilitySyntax, AvailableSyntax } from '../Syntax/Availability';
import { ComparisonConditionSyntax, ConditionSyntax } from '../Syntax/Conditions';
import { ContextExpressionSyntax, ExpressionSyntax } from '../Syntax/Expressions';
import { contextRoots } from './ExpressionParser';
import { ProvideSourceSyntax, ProvideSyntax, QueryProvideSourceSyntax } from '../Syntax/ProvideSyntax';
import { QuerySyntax } from '../Syntax/Queries';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax, SliceSyntax } from '../Syntax/Structure';
import { TypeRefSyntax } from '../Syntax/Declarations';
import { ParserContext } from './ParserContext';

// The names a provide may not use: the built-in $context members and the job-specific ones.
export const reservedProvideNames: ReadonlySet<string> = new Set(['tenant', 'identity', 'causedBy', 'causation', 'occurred', 'audiences', 'command', 'arguments']);

// The roots a $context path may start with besides the known ones, which no provide declares.
const reservedContextRoots = ['audiences'];

type Effective = ReadonlyMap<string, ProvideSyntax>;

// Checks the values added to $context by provide lines across the application, module, feature and slice levels.
// The port of the C# ProvideValidator.
export function validateProvides(application: ApplicationSyntax, context: ParserContext): void {
    new ContextPathValidator(context).visitApplication(application);
    const resolver = new AuthoringProductionResolver(application);
    const known = knownTypes(application, resolver);
    const validation = new Validation(resolver, known, context);
    const root = validation.declare(new Map(), application.provides ?? []);
    for (const module of application.modules) {
        const effective = validation.declare(root, module.provides ?? []);
        for (const feature of module.features) validation.feature(effective, feature);
    }
}

// The names of the provided values a source reads through $context.<name>.
export function provideReadNames(source: ProvideSourceSyntax): string[] {
    const expressions = source.kind === 'QueryProvideSourceSyntax' ? source.by : source.kind === 'ValueProvideSourceSyntax' ? [source.value] : [];
    return expressions.filter((expression): expression is ContextExpressionSyntax => expression.kind === 'ContextExpressionSyntax').map(expression => contextRoot(expression));
}

const contextRoot = (expression: ContextExpressionSyntax): string => expression.path.split('.')[0];

function knownTypes(application: ApplicationSyntax, resolver: AuthoringProductionResolver): Set<string> {
    const known = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', ...application.concepts.map(concept => concept.name), ...application.types.map(type => type.name), ...application.imports.map(imported => imported.qualifiedName.split('.').at(-1)!)]);
    for (const { slice } of resolver.slices) {
        for (const model of slice.readModels) known.add(model.name);
        for (const projection of slice.projections) {
            // Match C# ProjectionBuiltReadModelNames: direct variants replace the builder's own output.
            const variants = projection.blocks.filter(block => block.kind === 'ProjectionVariantSyntax');
            for (const name of variants.length > 0 ? variants.map(variant => variant.name) : [projection.readModel ?? projection.name]) known.add(name);
        }
        for (const reducer of dependencySourcesOf(slice).reducers ?? []) known.add(reducer.readModel);
    }
    return known;
}

const describe = (type: TypeRefSyntax): string => `${type.name}${type.isCollection ? '[]' : ''}${type.isOptional ? ' optional' : ''}`;
const sameType = (left: TypeRefSyntax, right: TypeRefSyntax): boolean => left.name === right.name && left.isCollection === right.isCollection && left.isOptional === right.isOptional;

function keyCount(query: QuerySyntax): number {
    if ((query.byParts?.length ?? 0) > 0) return query.byParts!.length;
    return query.by === null ? 0 : 1;
}

function reaches(effective: Effective, current: string, target: string, path: string[], seen: Set<string>): boolean {
    const provide = effective.get(current);
    if (provide === undefined) return false;
    for (const read of provideReadNames(provide.source).filter(name => effective.has(name))) {
        if (read === target) {
            path.push(read);
            return true;
        }
        if (seen.has(read)) continue;
        seen.add(read);
        path.push(read);
        if (reaches(effective, read, target, path, seen)) return true;
        path.pop();
    }
    return false;
}

class Validation {
    readonly #cycles = new Set<string>();

    constructor(readonly resolver: AuthoringProductionResolver, readonly known: ReadonlySet<string>, readonly context: ParserContext) {}

    feature(inherited: Effective, feature: FeatureSyntax): void {
        const effective = this.declare(inherited, feature.provides ?? []);
        for (const slice of feature.slices) this.declare(effective, slice.provides ?? []);
        for (const nested of feature.features) this.feature(effective, nested);
    }

    declare(inherited: Effective, provides: readonly ProvideSyntax[]): Effective {
        const effective = new Map(inherited);
        const declaredHere = new Set<string>();
        for (const provide of provides) {
            if (declaredHere.has(provide.name)) {
                this.context.error(DiagnosticCodes.DuplicateProvide, `'${provide.name}' is provided more than once at this level - declare each value once`, provide.location);
                continue;
            }
            declaredHere.add(provide.name);
            if (reservedProvideNames.has(provide.name)) {
                this.context.error(DiagnosticCodes.ReservedProvideName, `'${provide.name}' is a built-in $context value and cannot be provided - no level can change who a request is`, provide.location);
                continue;
            }
            const overridden = inherited.get(provide.name);
            if (overridden !== undefined && !sameType(overridden.type, provide.type)) {
                this.context.error(DiagnosticCodes.ProvideTypeMismatch, `'${provide.name}' is provided as ${describe(overridden.type)} higher up - an override keeps the type, not ${describe(provide.type)}`, provide.location);
            }
            if (!this.known.has(provide.type.name)) {
                this.context.warning(DiagnosticCodes.UnknownType, `Unknown type '${provide.type.name}' on provided value '${provide.name}'`, provide.type.location);
            }
            if (provide.source.kind === 'QueryProvideSourceSyntax') this.#validateQuery(provide, provide.source);
            effective.set(provide.name, provide);
        }
        this.#validateCycles(effective);
        return effective;
    }

    #validateQuery(provide: ProvideSyntax, source: QueryProvideSourceSyntax): void {
        const parts = source.query.split('.');
        const name = parts.at(-1)!;
        const qualifiers = parts.slice(0, -1);
        const queries = this.resolver.slices.flatMap(({ slice, scope }) => slice.queries.map(query => ({ query, scope })))
            .filter(entry => entry.query.name === name && qualifiers.length <= entry.scope.length && qualifiers.every((qualifier, index) => qualifier === entry.scope[entry.scope.length - qualifiers.length + index]));
        if (queries.length !== 1) {
            this.context.error(DiagnosticCodes.UnknownProvideQuery, `Unknown or ambiguous query '${source.query}' for provided value '${provide.name}'`, source.location);
            return;
        }
        const query = queries[0].query;
        if (query.isObservable) {
            this.context.error(DiagnosticCodes.ObservableProvideQuery, `Query '${source.query}' is observable - a provided value is resolved once per request, so it needs a snapshot query`, source.location);
        }
        const keys = keyCount(query);
        if (source.by.length !== keys) {
            this.context.error(DiagnosticCodes.ProvideQueryTypeMismatch, `Query '${source.query}' takes ${keys} key value(s) but 'by' supplies ${source.by.length}`, source.location);
        }
        if (query.returnType.name !== provide.type.name || query.returnType.isCollection !== provide.type.isCollection) {
            this.context.error(DiagnosticCodes.ProvideQueryTypeMismatch, `Provided value '${provide.name}' is ${describe(provide.type)} but query '${source.query}' returns ${describe(query.returnType)}`, source.location);
        }
    }

    #validateCycles(effective: Effective): void {
        for (const start of effective.values()) {
            const path: string[] = [];
            const key = `${start.location.path ?? ''}:${start.location.line}:${start.location.column}`;
            if (reaches(effective, start.name, start.name, path, new Set()) && !this.#cycles.has(key)) {
                this.#cycles.add(key);
                this.context.error(DiagnosticCodes.ProvideCycle, `Provided value '${start.name}' reads itself through ${[start.name, ...path].join(' -> ')}`, start.location);
            }
        }
    }
}

const comparisonsOf = (condition: ConditionSyntax): readonly ComparisonConditionSyntax[] =>
    condition.kind === 'ComparisonConditionSyntax' ? [condition] : [...comparisonsOf(condition.left), ...comparisonsOf(condition.right)];

// Every name any provide in the application declares, at any level.
function providedEverywhere(application: ApplicationSyntax): ReadonlySet<string> {
    const names = new Set<string>();
    new class extends ScreenplaySyntaxWalker {
        override visitProvide(syntax: ProvideSyntax): void {
            names.add(syntax.name);
            super.visitProvide(syntax);
        }
    }().visitApplication(application);
    return names;
}

// Withdraws the parser's unknown $context.<root> warning for a root that a provide in scope names, once the whole
// application is assembled and every level's provided values are known.
class ContextPathValidator extends ScreenplaySyntaxWalker {
    readonly #scopes: ReadonlySet<string>[] = [];
    #everywhere: ReadonlySet<string> = new Set();
    #gateOwner: string | null = null;

    constructor(readonly context: ParserContext) { super(); }

    override visitApplication(syntax: ApplicationSyntax): void {
        this.#everywhere = providedEverywhere(syntax);
        this.#scoped(syntax.provides, () => super.visitApplication(syntax));
    }

    override visitAvailable(syntax: AvailableSyntax): void {
        this.#gateOwner = 'available when';
        super.visitAvailable(syntax);
        this.#gateOwner = null;
    }

    // A reusable block is read wherever it is used, so a value it reads has to be provided somewhere.
    override visitAvailability(syntax: AvailabilitySyntax): void {
        this.#gateOwner = `availability '${syntax.name}'`;
        super.visitAvailability(syntax);
        this.#gateOwner = null;
    }

    override visitAvailabilityRequirement(syntax: AvailabilityRequirementSyntax): void {
        super.visitAvailabilityRequirement(syntax);
        if (syntax.kind === 'ConditionAvailabilityRequirementSyntax') {
            for (const comparison of comparisonsOf(syntax.condition)) this.#checkLeft(comparison);
        }
    }
    override visitModule(syntax: ModuleSyntax): void { this.#scoped(syntax.provides, () => super.visitModule(syntax)); }
    override visitFeature(syntax: FeatureSyntax): void { this.#scoped(syntax.provides, () => super.visitFeature(syntax)); }
    override visitSlice(syntax: SliceSyntax): void { this.#scoped(syntax.provides, () => super.visitSlice(syntax)); }

    override visitExpression(syntax: ExpressionSyntax): void {
        super.visitExpression(syntax);
        if (syntax.kind !== 'ContextExpressionSyntax') return;
        const root = contextRoot(syntax);
        if (contextRoots.includes(root)) return;
        // The parser warned about every root it did not know; a root a provide in scope names is withdrawn here, once every level is known.
        if (this.#isProvided(root)) {
            this.context.withdraw(DiagnosticCodes.UnknownContextPath, syntax.location, `'${syntax.path}'`);
            return;
        }
        if (this.#gateOwner !== null) {
            this.context.withdraw(DiagnosticCodes.UnknownContextPath, syntax.location, `'${syntax.path}'`);
            this.#reportUnprovided(this.#gateOwner, root, syntax.path, syntax.location);
        }
    }

    #isProvided(root: string): boolean {
        return reservedContextRoots.includes(root)
            || this.#scopes.some(scope => scope.has(root))
            || (this.#gateOwner?.startsWith('availability') === true && this.#everywhere.has(root));
    }

    #checkLeft(comparison: ComparisonConditionSyntax): void {
        const prefix = '$context.';
        if (!comparison.left.startsWith(prefix)) return;
        const path = comparison.left.substring(prefix.length);
        const root = path.split('.')[0];
        if (this.#gateOwner !== null && !contextRoots.includes(root) && !this.#isProvided(root)) this.#reportUnprovided(this.#gateOwner, root, path, comparison.location);
    }

    // A condition that reads a value nothing provides can never be decided, and an undecided gate stays closed.
    #reportUnprovided(owner: string, root: string, path: string, location: SourceLocation): void {
        const where = owner.startsWith('availability') ? 'anywhere in the application' : 'here';
        this.context.warning(
            DiagnosticCodes.UnprovidedAvailabilityValue,
            `'${owner}' reads '$context.${path}' but nothing provides '${root}' ${where} - declare it with 'provide ${root} <Type> ...'`,
            location);
    }

    #scoped(provides: readonly ProvideSyntax[] | undefined, visit: () => void): void {
        this.#scopes.push(new Set((provides ?? []).map(provide => provide.name)));
        visit();
        this.#scopes.pop();
    }
}
