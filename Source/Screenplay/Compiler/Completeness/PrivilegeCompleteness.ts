// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { RefusalDeclarations } from '../Parsing/RefusalDeclarations';
import { AuthorizeSyntax, PolicyRequirementSyntax } from '../Syntax/Authorization';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { CaptureAppendSyntax } from '../Syntax/Captures';
import { CommandSyntax } from '../Syntax/Commands';
import { EventSyntax } from '../Syntax/Declarations';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { combine, conditionTruth, policyOf, requirementTruth } from './AuthorizationFacts';

interface Producer {
    readonly event: EventSyntax;
    readonly name: string;
    readonly location: SourceLocation;
    readonly gates: readonly AuthorizeSyntax[];
}

function commandGates(application: ApplicationSyntax): Map<CommandSyntax, readonly AuthorizeSyntax[]> {
    const result = new Map<CommandSyntax, readonly AuthorizeSyntax[]>();
    const collect = (features: readonly FeatureSyntax[], inherited: readonly AuthorizeSyntax[]): void => {
        for (const feature of features) {
            const gates = feature.authorize === null ? inherited : [...inherited, feature.authorize];
            for (const command of feature.slices.flatMap(slice => slice.commands)) result.set(command, command.authorize === null ? gates : [...gates, command.authorize]);
            collect(feature.features, gates);
        }
    };
    for (const module of application.modules) collect(module.features, module.authorize === null ? [] : [module.authorize]);
    return result;
}

class CaptureAppends extends ScreenplaySyntaxWalker {
    readonly appends: CaptureAppendSyntax[] = [];
    override visitCaptureAppend(syntax: CaptureAppendSyntax): void { this.appends.push(syntax); }
}

export function privilegeDiagnostics(application: ApplicationSyntax): readonly Diagnostic[] {
    const declarations = new RefusalDeclarations(application);
    const productions = new AuthoringProductionResolver(application);
    const gates = commandGates(application);
    const producers: Producer[] = [];
    for (const { slice } of declarations.slices) {
        const add = (eventName: string, name: string, location: SourceLocation, gates: readonly AuthorizeSyntax[] = []): void => {
            const event = declarations.event(eventName, slice);
            if (event !== null && !producers.some(producer => producer.event === event && producer.name === name && producer.location === location)) producers.push({ event, name, location, gates });
        };
        for (const command of slice.commands) {
            for (const produced of command.produces.filter(produced => productions.isEventProduction(produced, slice))) add(produced.event, `command '${command.name}'`, command.location, gates.get(command)!);
        }
        for (const capture of slice.captures) {
            const walker = new CaptureAppends();
            walker.visitCapture(capture);
            for (const append of walker.appends) add(append.event, `capture '${capture.name}'`, capture.location);
        }
        for (const reaction of slice.reactions) {
            for (const trigger of reaction.triggers) {
                const produced = [...trigger.produces, ...trigger.invokes.flatMap(invocation => invocation.onRefused?.flatMap(branch => branch.produces) ?? [])];
                for (const production of produced) add(production.event, `reaction '${reaction.name}'`, reaction.location);
            }
        }
    }
    const policies = application.policies ?? [];
    const opaque = (requirement: PolicyRequirementSyntax): boolean => requirement.kind === 'LogicalPolicyRequirementSyntax'
        ? opaque(requirement.left) || opaque(requirement.right) : policyOf(requirement.name, policies) === null;
    const diagnostics: Diagnostic[] = [];
    for (const { slice } of declarations.slices) {
        for (const reaction of slice.reactions) {
            const roles = reaction.runsAs?.roles ?? [];
            if (roles.length === 0) continue;
            for (const trigger of reaction.triggers) {
                if (trigger.source.kind !== 'NamedTriggerSourceSyntax') continue;
                const event = declarations.event(trigger.source.name, slice);
                if (event === null) continue;
                for (const producer of producers.filter(producer => producer.event === event)) {
                    if (producer.gates.some(gate => opaque(gate.requirement))) {
                        diagnostics.push({ severity: 'information', code: DiagnosticCodes.ReactionPrivilegeEscalation,
                            message: `The effective gate of ${producer.name}, producing '${event.name}' for reaction '${reaction.name}', cannot be compared with its declared system roles.`, location: producer.location });
                    } else if (roles.some(role => producer.gates.reduce<boolean | null>((truth, gate) => combine(truth, 'And', requirementTruth(gate.requirement, policies,
                        condition => conditionTruth(condition, atom => atom.kind === 'RoleConditionSyntax' && atom.role === role ? false : null))), true) !== false)) {
                        diagnostics.push({ severity: 'warning', code: DiagnosticCodes.ReactionPrivilegeEscalation,
                            message: `Unprivileged ${producer.name} produces '${event.name}' for elevated reaction '${reaction.name}'; its effective gate does not require every declared system role.`, location: producer.location });
                    }
                }
            }
        }
    }
    return diagnostics;
}
