// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { AgentOperationKind, AgentOperationSyntax } from '../Syntax/Agents';
import type { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import type { ParserContext } from './ParserContext';
import { isPerformedQuery } from './QueryParser';

interface Target {
    readonly kind: AgentOperationKind;
    readonly name: string;
    readonly authorized: boolean;
    readonly global?: boolean;
    readonly refusedKind?: string;
}

export function validateAgents(application: ApplicationSyntax, context: ParserContext, deferImportedReferences = false): void {
    const agents = application.agents;
    if (agents == null) return;
    if (agents.tokens === null) context.error(DiagnosticCodes.AgentTokensMissing, "An agents block requires exactly one 'tokens personal' declaration", agents.location);
    else if (agents.tokens.syntaxKind !== 'Personal') context.error(DiagnosticCodes.UnsupportedAgentTokenKind, "Only 'tokens personal' is supported for agents", agents.tokens.location);
    const targets = application.modules.flatMap(module => module.features.flatMap(feature => inFeature(feature, [module.name], module.authorize !== null)));
    const exposed = new Set<string>();
    const deferred = deferImportedReferences && application.fileImports.length > 0;
    for (const operation of agents.operations) {
        validateFields(operation, context);
        if (!deferred) validateReference(operation, targets, exposed, context);
    }
    if (deferred || context.diagnostics.some(diagnostic => diagnostic.severity === 'error')) return;
    if (agents.operations.length === 0) context.warning(DiagnosticCodes.AgentsExposeNothing, 'The agents block exposes no operations', agents.location);
    const unexposed = targets.filter(target => !exposed.has(`${target.kind}:${target.name}`)).map(target => target.name).sort();
    if (unexposed.length > 0) {
        const suffix = unexposed.length > 10 ? `, and ${unexposed.length - 10} more` : '';
        context.information(DiagnosticCodes.UnexposedAgentOperations, `${unexposed.length} of ${targets.length} queries and commands are not exposed to agents: ${unexposed.slice(0, 10).join(', ')}${suffix}`, agents.location);
    }
}

function inFeature(feature: FeatureSyntax, parents: readonly string[], authorized: boolean): Target[] {
    const path = [...parents, feature.name];
    const gated = authorized || feature.authorize !== null;
    const targets = feature.slices.flatMap(slice => {
        const name = (operation: string) => [...path, slice.name, operation].join('.');
        return [
            ...slice.commands.map(command => ({ kind: 'Command' as const, name: name(command.name), authorized: gated || command.authorize !== null })),
            ...slice.queries.map(query => ({ kind: 'Query' as const, name: name(query.name), authorized: gated || query.authorize !== null, global: query.scope === 'global', refusedKind: isPerformedQuery(query) ? 'performed query' : undefined })),
            ...(slice.graphQueries ?? []).map(query => ({ kind: 'Query' as const, name: name(query.name), authorized: gated || query.authorize != null, refusedKind: 'graph query' }))
        ];
    });
    return [...targets, ...feature.features.flatMap(child => inFeature(child, path, gated))];
}

function validateFields(operation: AgentOperationSyntax, context: ParserContext): void {
    if (operation.description === null || operation.description.trim().length === 0) context.error(DiagnosticCodes.AgentOperationMissingDescription, 'An exposed operation requires a nonempty literal description', operation.location);
    if (operation.scope === null) context.error(DiagnosticCodes.AgentOperationMissingScope, "An exposed operation requires exactly one 'scope read' or 'scope write'", operation.location);
    else if (operation.syntaxKind === 'Command' && operation.scope !== 'Write') context.error(DiagnosticCodes.AgentCommandRequiresWriteScope, "An exposed command requires 'scope write'", operation.location);
}

function validateReference(operation: AgentOperationSyntax, targets: readonly Target[], exposed: Set<string>, context: ParserContext): void {
    const candidates = targets.filter(target => target.kind === operation.syntaxKind && (target.name === operation.reference || target.name.endsWith(`.${operation.reference}`)));
    if (candidates.length === 0) {
        context.error(DiagnosticCodes.UnknownAgentOperation, `Unknown agent ${operation.syntaxKind.toLowerCase()} '${operation.reference}'`, operation.location);
        return;
    }
    if (candidates.length > 1) {
        context.error(DiagnosticCodes.AmbiguousAgentOperation, `Ambiguous agent operation '${operation.reference}' - qualify one of: ${candidates.map(target => target.name).join(', ')}`, operation.location);
        return;
    }
    const target = candidates[0];
    const key = `${target.kind}:${target.name}`;
    if (exposed.has(key)) context.error(DiagnosticCodes.DuplicateAgentOperation, `Agent operation '${target.name}' is exposed more than once`, operation.location);
    exposed.add(key);
    if (target.refusedKind !== undefined) context.error(DiagnosticCodes.UnsupportedAgentQueryKind, `Query '${target.name}' is a ${target.refusedKind} and cannot be exposed to agents yet`, operation.location);
    if (target.global === true) context.error(DiagnosticCodes.AgentGlobalQueryRefused, `Global query '${target.name}' cannot be exposed to agents`, operation.location);
    if (!target.authorized) context.warning(DiagnosticCodes.UngatedAgentOperation, `Exposed ${operation.syntaxKind.toLowerCase()} '${target.name}' has no authorization - any token holder can call it`, operation.location);
}
