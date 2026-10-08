// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { InvocationRefusalSyntax } from '../Syntax/InvocationRefusalSyntax';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { RefusalDeclarations } from './RefusalDeclarations';
import { RefusalValueWalker } from './RefusalValueWalker';
import { expandSpecificationExamples } from './SpecificationCommandExamples';

// Invocations have no identity declaration until #383; keep that decision separate from authorization gating.
function invocationHasNoDeclaredIdentity(): boolean { return true; }

function authorizationGatedSlices(application: ApplicationSyntax): Set<SliceSyntax> {
    const slices = new Set<SliceSyntax>();
    const collect = (feature: FeatureSyntax, inherited: boolean): void => {
        const gated = inherited || feature.authorize !== null;
        if (gated) feature.slices.forEach(slice => slices.add(slice));
        feature.features.forEach(child => collect(child, gated));
    };
    application.modules.forEach(module => module.features.forEach(feature => collect(feature, module.authorize !== null)));
    return slices;
}

export function validateReactionRefusals(application: ApplicationSyntax, context: ParserContext): void {
    const declarations = new RefusalDeclarations(expandSpecificationExamples(application));
    new RefusalValueWalker(declarations, context).visitApplication(declarations.application);
    const gatedSlices = authorizationGatedSlices(declarations.application);
    for (const { slice } of declarations.slices) {
        const constraintOf = (name: string) => declarations.resolve(name, slice, owner => owner.constraints);
        const sameConstraint = (left: string, right: string) => left === right || constraintOf(left) !== null && constraintOf(left)?.node === constraintOf(right)?.node;
        const covers = (previous: InvocationRefusalSyntax, branch: InvocationRefusalSyntax) =>
            previous.selector === 'any' && ['any', 'validation', 'constraint'].includes(branch.selector) ||
            previous.selector === branch.selector && (previous.selector !== 'constraint' || previous.constraint === null || branch.constraint !== null && sameConstraint(previous.constraint, branch.constraint));
        for (const invocation of slice.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.invokes)) {
            const invoked = declarations.resolve(invocation.command, slice, owner => owner.commands);
            const authorizationGated = invoked !== null && (invoked.node.authorize !== null || gatedSlices.has(invoked.slice));
            const earlier: InvocationRefusalSyntax[] = [];
            for (const branch of invocation.onRefused ?? []) {
                if (earlier.some(previous => covers(previous, branch))) context.warning(DiagnosticCodes.UnreachableRefusalBranch, 'This refusal selector is covered by an earlier branch; the first matching branch wins.', branch.location);
                earlier.push(branch);
                if (branch.selector === 'authorization' && invocationHasNoDeclaredIdentity() && authorizationGated) {
                    context.warning(DiagnosticCodes.AuthorizationRefusalWithoutIdentity,
                        `Command '${invocation.command}' is authorization-gated, but this invocation has no declared identity. This authorization refusal branch always fires in the reference runner because there is no caller; Arc runs reactor commands as the system. Declare an invoking identity once supported (#383).`,
                        branch.location);
                }
                if (branch.constraint === null) continue;
                const constraint = constraintOf(branch.constraint);
                if (constraint === null) {
                    context.error(DiagnosticCodes.UnknownRefusalConstraint, `Unknown or ambiguous refusal constraint '${branch.constraint}'.`, branch.location);
                    continue;
                }
                if (invoked === null || invoked.node.handler !== null) continue;
                const rules = [constraint.node, ...constraint.node.additionalRules];
                if (rules.some(rule => rule.kind === 'FileConstraintSyntax')) continue;
                const targets = rules.map(rule => rule.kind === 'FileConstraintSyntax' ? null : declarations.event(rule.event, constraint.slice));
                const productions = invoked.node.produces.map(produced => declarations.event(produced.event, invoked.slice));
                if (targets.every(target => target !== null) && productions.every(produced => produced !== null) && !targets.some(target => productions.includes(target)))
                    context.warning(DiagnosticCodes.UnreachableRefusalBranch, `Constraint '${branch.constraint}' targets none of the events produced by '${invocation.command}'.`, branch.location);
            }
        }
    }
}
