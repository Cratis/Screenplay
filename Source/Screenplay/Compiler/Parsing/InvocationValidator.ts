// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { TypeRefSyntax } from '../Syntax/Declarations';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { WorkflowNodeSyntax, WorkflowSyntax } from '../Syntax/Lifecycles';
import { operationDeclarations } from '../Syntax/Operations';
import { ReactionTriggerSyntax } from '../Syntax/Reactions';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SpecificationAbilityAnswerSyntax } from '../Syntax/Specifications';
import { ApplicationSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { AbilitySyntax } from '../Syntax/SystemSyntax';
import { ParserContext } from './ParserContext';

const reservedOutlets: ReadonlySet<string> = new Set(['next', 'refused', 'failed', 'returned', 'expired']);
const valueless: ReadonlySet<string> = new Set(['failed', 'expired']);

const isOutcome = (branch: string | undefined | null): boolean => branch != null && branch !== 'returned' && branch !== 'failed';
const isAbilityTarget = (target: string | null | undefined): boolean => target?.includes('.') === true;

// The port of C# InvocationValidator: every ability invocation site names a declared ability, supplies its inputs and uses only outcomes it declares.
// A trigger that carries an attached body is not modeled by this compiler, so 'PLAY1014' is reported by the C# compiler only.
export function validateInvocations(application: ApplicationSyntax, context: ParserContext): void {
    const concepts = new Map<string, string>();
    for (const concept of application.concepts) if (!concepts.has(concept.name)) concepts.set(concept.name, concept.type);
    const abilities = new Map<string, AbilitySyntax>();
    for (const system of application.systems ?? []) {
        for (const ability of system.abilities) if (!abilities.has(`${system.name}.${ability.name}`)) abilities.set(`${system.name}.${ability.name}`, ability);
    }

    const slices = new AuthoringProductionResolver(application).slices;
    for (const { slice } of slices) {
        for (const trigger of slice.reactions.flatMap(reaction => reaction.triggers)) validateTrigger(trigger, abilities, concepts, context);
        for (const phase of operationDeclarations(slice).flatMap(operation => [operation.execute, operation.compensate])) {
            for (const invocation of phase?.invocations ?? []) validateSite(invocation.command, invocation.mappings ?? [], invocation.location, abilities, concepts, context);
        }
    }

    for (const { slice } of slices) {
        for (const answer of slice.specifications.flatMap(specification => specification.givenAbilityAnswers ?? [])) validateAnswer(answer, abilities, context);
    }

    for (const machine of application.stateMachines ?? []) {
        for (const entry of machine.states.flatMap(state => state.entering).filter(candidate => candidate.entryKind === 'Invoke')) {
            const ability = validateSite(entry.target, entry.mappings ?? [], entry.location, abilities, concepts, context);
            if (ability?.answersLater === true) {
                context.error(DiagnosticCodes.AnswersLaterInStateMachine, `'${entry.target}' answers later; a state machine has no data to hold the answer - start a workflow instead.`, entry.location);
            }
        }
    }

    for (const workflow of application.workflows ?? []) {
        validateWorkflow(workflow, abilities, concepts, context);
        validateDominance(workflow, context);
    }

    new InvocationValueScopeWalker(context).visitApplication(application);
}

function validateAnswer(answer: SpecificationAbilityAnswerSyntax, abilities: ReadonlyMap<string, AbilitySyntax>, context: ParserContext): void {
    const ability = abilities.get(answer.ability);
    if (ability === undefined) {
        context.error(DiagnosticCodes.UnknownInvocationTarget, `'${answer.ability}' is not a declared system ability.`, answer.location);
        return;
    }
    if (answer.answer === 'answers' && !ability.outcomes.some(outcome => outcome.name === answer.outcome)) {
        context.error(DiagnosticCodes.UnknownInvocationOutcome, `'${answer.ability}' declares no outcome '${answer.outcome}'.`, answer.location);
    }
    for (const value of answer.values.filter(candidate => !ability.returns.some(field => field.name === candidate.property))) {
        context.error(DiagnosticCodes.InvalidAbilityAnswer, `'${answer.ability}' returns no '${value.property}'.`, value.location);
    }
}

function validateTrigger(trigger: ReactionTriggerSyntax, abilities: ReadonlyMap<string, AbilitySyntax>, concepts: ReadonlyMap<string, string>, context: ParserContext): void {
    for (const invocation of trigger.invokes ?? []) {
        let ability: AbilitySyntax | undefined;
        if (isAbilityTarget(invocation.command)) {
            ability = validateSite(invocation.command, invocation.mappings ?? [], invocation.location, abilities, concepts, context);
            if (ability === undefined) continue;
            if (ability.answersLater) {
                context.error(DiagnosticCodes.AnswersLaterInReaction, `'${invocation.command}' answers later; a reaction has no place to wait - invoke it from a workflow.`, invocation.location);
            }
            if (dependencySourcesOf(trigger)?.implementation === true) {
                context.error(DiagnosticCodes.InvocationWithAttachedBody, `A trigger that invokes '${invocation.command}' cannot also carry an attached body.`, invocation.location);
            }
        }
        const seen = new Set<string>();
        for (const branch of invocation.branches ?? []) {
            if (seen.has(branch.selector)) {
                context.error(DiagnosticCodes.DuplicateInvocationBranch, `'${invocation.command}' handles 'on ${branch.selector}' more than once.`, branch.location);
            } else if (isOutcome(branch.selector) && ability?.outcomes.some(outcome => outcome.name === branch.selector) !== true) {
                context.error(DiagnosticCodes.UnknownInvocationOutcome, `'${invocation.command}' declares no outcome '${branch.selector}'.`, branch.location);
            }
            seen.add(branch.selector);
        }
    }
}

function validateWorkflow(workflow: WorkflowSyntax, abilities: ReadonlyMap<string, AbilitySyntax>, concepts: ReadonlyMap<string, string>, context: ParserContext): void {
    for (const node of workflow.nodes) {
        if (node.nodeKind === 'Action' && isAbilityTarget(node.command)) validateAction(node, abilities, concepts, context);
        else if (node.nodeKind === 'Approval' && node.via != null) validateApproval(node, abilities, context);
        else if (node.nodeKind === 'Compute') validateCompute(node, context);
    }
}

function validateAction(node: WorkflowNodeSyntax, abilities: ReadonlyMap<string, AbilitySyntax>, concepts: ReadonlyMap<string, string>, context: ParserContext): void {
    const ability = validateSite(node.command!, node.mappings ?? [], node.location, abilities, concepts, context);
    if (ability === undefined) return;
    if ((node.attempts ?? 0) > 1 && (ability.effectClass ?? 'safe') === 'once') {
        context.warning(DiagnosticCodes.OnceAbilityRetried, `'${node.command}' is a 'once' ability invoked with ${node.attempts} attempts; a repeated attempt can repeat its effect.`, node.location);
    }
    const seen = new Set<string>();
    for (const edge of node.edges) {
        if (seen.has(edge.outlet)) continue;
        seen.add(edge.outlet);
        if (edge.outlet === 'expired') {
            if (!ability.answersLater || node.expiresAfter === null) {
                context.error(DiagnosticCodes.UnreachableInvocationOutlet, `Outlet 'expired' of '${node.name}' is never taken: '${node.command}' must answer later and the action must say 'expires after'.`, edge.location);
            }
        } else if (isOutcome(edge.outlet) && edge.outlet !== 'next' && !ability.outcomes.some(outcome => outcome.name === edge.outlet)) {
            context.error(DiagnosticCodes.UnknownInvocationOutcome, `'${node.command}' declares no outcome '${edge.outlet}'.`, edge.location);
        }
    }
}

function validateApproval(node: WorkflowNodeSyntax, abilities: ReadonlyMap<string, AbilitySyntax>, context: ParserContext): void {
    const ability = abilities.get(node.via!);
    if (ability === undefined) {
        context.error(DiagnosticCodes.UnknownInvocationTarget, `Approval '${node.name}' is asked via '${node.via}', which is not a declared ability.`, node.location);
        return;
    }
    const outcomes = new Set(ability.outcomes.map(outcome => outcome.name.toLowerCase()));
    if (!ability.answersLater || !outcomes.has('approved') || !outcomes.has('rejected')) {
        context.error(DiagnosticCodes.ApprovalViaAbilityLacksOutcomes, `'${node.via}' must answer later with the outcomes 'approved' and 'rejected' to be used for approval '${node.name}'.`, node.location);
    }
}

function validateCompute(node: WorkflowNodeSyntax, context: ParserContext): void {
    if ((node.returns ?? []).length === 0) context.error(DiagnosticCodes.InvalidComputeDeclaration, `Compute '${node.name}' declares no 'returns'.`, node.location);
    if (node.code == null && node.file == null) context.error(DiagnosticCodes.InvalidComputeDeclaration, `Compute '${node.name}' has no body: add a tagged fence or a file.`, node.location);
    const outlets = [...new Set(node.edges.map(edge => edge.outlet))];
    const single = outlets.length === 1 && outlets[0] === 'next';
    const named = outlets.length >= 2 && outlets.every(outlet => !reservedOutlets.has(outlet.toLowerCase()));
    if (!single && !named) {
        context.error(DiagnosticCodes.InvalidComputeDeclaration, `Compute '${node.name}' has either one 'next' outlet or two or more author-named outlets; reserved outlet names are not allowed.`, node.location);
    }
}

function validateSite(
    target: string,
    mappings: readonly PropertyMappingSyntax[],
    location: SourceLocation,
    abilities: ReadonlyMap<string, AbilitySyntax>,
    concepts: ReadonlyMap<string, string>,
    context: ParserContext
): AbilitySyntax | undefined {
    const ability = abilities.get(target);
    if (ability === undefined) {
        if (isAbilityTarget(target)) context.error(DiagnosticCodes.UnknownInvocationTarget, `'${target}' is not a declared system ability.`, location);
        return undefined;
    }
    const supplied = new Set<string>();
    const inputs = new Map<string, AbilitySyntax['inputs'][number]>();
    for (const input of ability.inputs) if (!inputs.has(input.name)) inputs.set(input.name, input);
    for (const mapping of mappings) {
        const input = inputs.get(mapping.property);
        if (input === undefined) {
            context.error(DiagnosticCodes.UnknownAbilityInput, `'${target}' declares no input '${mapping.property}'.`, mapping.location);
        } else if (supplied.has(mapping.property)) {
            context.error(DiagnosticCodes.UnknownAbilityInput, `Input '${mapping.property}' of '${target}' is mapped more than once.`, mapping.location);
        } else if (!literalFits(mapping.source, input.type, concepts)) {
            context.error(DiagnosticCodes.IncompatibleAbilityInputMapping, `The value mapped to '${mapping.property}' cannot be a ${input.type.name}.`, mapping.location);
        }
        supplied.add(mapping.property);
    }
    for (const input of inputs.values()) {
        if (!input.type.isOptional && !supplied.has(input.name)) context.error(DiagnosticCodes.MissingAbilityInput, `'${target}' needs input '${input.name}'.`, location);
    }
    return ability;
}

function literalFits(source: ExpressionSyntax, type: TypeRefSyntax, concepts: ReadonlyMap<string, string>): boolean {
    if (source.kind !== 'LiteralExpressionSyntax' || source.value === null || type.isCollection) return true;
    const value = source.value;
    switch (concepts.get(type.name) ?? type.name) {
        case 'String': case 'Uuid': case 'Date': case 'DateTime': return typeof value === 'string';
        case 'Bool': return typeof value === 'boolean';
        case 'Int': case 'Decimal': return typeof value !== 'string' && typeof value !== 'boolean';
        default: return true;
    }
}

// Node-result reads: every path to the reader passes the producing node through an outlet that carries a returned value.
function validateDominance(workflow: WorkflowSyntax, context: ParserContext): void {
    const nodes = new Map<string, WorkflowNodeSyntax>();
    for (const node of workflow.nodes) if (!nodes.has(node.name)) nodes.set(node.name, node);
    const producers = new Set([...nodes.values()].filter(node => node.nodeKind === 'Compute' || (node.nodeKind === 'Action' && isAbilityTarget(node.command))).map(node => node.name));
    if (producers.size === 0) return;
    const roots = workflow.nodes.filter(node => node.nodeKind === 'Trigger').map(node => node.name);
    if (roots.length === 0 && workflow.nodes.length > 0) roots.push(workflow.nodes[0].name);
    for (const reader of workflow.nodes) {
        for (const { path, location } of reads(reader)) {
            const root = path.split('.')[0];
            if (!producers.has(root)) continue;
            if (root === reader.name || reachableWithoutValues(nodes, roots, root, reader.name)) {
                context.error(DiagnosticCodes.NonDominatingNodeRead, `Node '${reader.name}' reads '${path}', but not every path to it passes '${root}' with a returned value.`, location);
            }
        }
    }
}

function reads(reader: WorkflowNodeSyntax): { path: string; location: SourceLocation }[] {
    const found: { path: string; location: SourceLocation }[] = [];
    for (const mapping of reader.mappings ?? []) if (mapping.source.kind === 'PathExpressionSyntax') found.push({ path: mapping.source.path, location: mapping.location });
    if (reader.condition !== null) found.push(...conditionReads(reader.condition));
    return found;
}

function conditionReads(condition: NonNullable<WorkflowNodeSyntax['condition']>): { path: string; location: SourceLocation }[] {
    if (condition.kind === 'ComparisonConditionSyntax') {
        const found = [{ path: condition.left, location: condition.location }];
        if (condition.right.kind === 'PathExpressionSyntax') found.push({ path: condition.right.path, location: condition.location });
        return found;
    }
    return [...conditionReads(condition.left), ...conditionReads(condition.right)];
}

function reachableWithoutValues(nodes: ReadonlyMap<string, WorkflowNodeSyntax>, roots: readonly string[], producer: string, target: string): boolean {
    const seen = new Set<string>();
    const stack = [...roots];
    while (stack.length > 0) {
        const name = stack.pop()!;
        if (seen.has(name)) continue;
        seen.add(name);
        if (name === target) return true;
        const node = nodes.get(name);
        if (node === undefined) continue;
        for (const edge of node.edges) {
            if (name === producer && !valueless.has(edge.outlet)) continue;
            stack.push(edge.target);
        }
    }
    return false;
}

class InvocationValueScopeWalker extends ScreenplaySyntaxWalker {
    private branch: string | null = null;
    private inWorkflow = false;

    constructor(private readonly context: ParserContext) { super(); }

    override visitInvocationBranch(syntax: Parameters<ScreenplaySyntaxWalker['visitInvocationBranch']>[0]): void {
        const previous = this.branch;
        this.branch = syntax.selector;
        super.visitInvocationBranch(syntax);
        this.branch = previous;
    }

    override visitWorkflowNode(syntax: WorkflowNodeSyntax): void {
        const previous = this.inWorkflow;
        this.inWorkflow = true;
        super.visitWorkflowNode(syntax);
        this.inWorkflow = previous;
    }

    override visitNode(node: SyntaxNode): void {
        if (node.kind !== 'InvocationValueExpressionSyntax') return;
        const value = node as unknown as { source: string; member: string; location: SourceLocation };
        let inScope: boolean;
        let where: string;
        switch (value.source) {
            case 'result': inScope = this.branch === 'returned'; where = "only in an 'on returned' branch"; break;
            case 'failure': inScope = this.branch === 'failed'; where = "only in an 'on failed' branch"; break;
            case 'outcome': inScope = this.branch !== null && this.branch !== 'returned' && this.branch !== 'failed'; where = "only in an 'on <Outcome>' branch"; break;
            default: inScope = this.inWorkflow; where = 'only in a workflow node'; break;
        }
        if (!inScope) this.context.error(DiagnosticCodes.InvocationValueOutsideBranch, `'$${value.source}' is available ${where}.`, value.location);
    }
}
