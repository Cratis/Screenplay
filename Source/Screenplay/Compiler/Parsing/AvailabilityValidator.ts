// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AvailabilityRequirementSyntax, AvailabilitySyntax, AvailableSyntax, availabilityLeaves } from '../Syntax/Availability';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax, SliceSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { audienceLocationKey } from './AvailabilityParser';
import { ParserContext } from './ParserContext';

const maximumAudiences = 12;

type Blocks = ReadonlyMap<string, AvailabilitySyntax>;

// Checks the audience and availability declarations and the gates that name them. The port of the C# AvailabilityValidator.
export function validateAvailability(application: ApplicationSyntax, context: ParserContext): void {
    const audiences = declare(application.audiences ?? [], 'audience', DiagnosticCodes.DuplicateAudience, context);
    const availabilities = declare(application.availabilities ?? [], 'availability', DiagnosticCodes.DuplicateAvailability, context);
    for (const persona of application.personas) {
        const named = persona.audiences ?? [];
        named.forEach((audience, index) => {
            if (audiences.has(audience)) return;
            const location = persona.directiveLocations?.[audienceLocationKey(named, index)] ?? persona.location;
            context.warning(DiagnosticCodes.UnknownAudience, `Unknown audience '${audience}' - declare it with 'audience ${audience}'`, location);
        });
    }
    for (const availability of application.availabilities ?? []) validateReferences(availability.requirements, audiences, availabilities, context);
    for (const gate of gatesOf(application)) validateReferences([gate.requirement], audiences, availabilities, context);
    reportCycles(application.availabilities ?? [], availabilities, context);
}

function gatesOf(application: ApplicationSyntax): readonly AvailableSyntax[] {
    const gates: AvailableSyntax[] = [];
    new class extends ScreenplaySyntaxWalker {
        override visitAvailable(syntax: AvailableSyntax): void {
            gates.push(syntax);
            super.visitAvailable(syntax);
        }
    }().visitApplication(application);
    return gates;
}

function declare(declarations: readonly (SyntaxNode & { readonly name: string })[], keyword: string, code: string, context: ParserContext): ReadonlySet<string> {
    const declared = new Set<string>();
    for (const declaration of declarations) {
        if (declared.has(declaration.name)) {
            context.error(code, `Duplicate ${keyword} '${declaration.name}' - declare each ${keyword} once`, declaration.location);
        } else {
            declared.add(declaration.name);
        }
    }
    return declared;
}

function validateReferences(
    requirements: readonly AvailabilityRequirementSyntax[],
    audiences: ReadonlySet<string>,
    availabilities: ReadonlySet<string>,
    context: ParserContext): void {
    for (const requirement of requirements) {
        for (const audience of availabilityLeaves(requirement, 'AudienceAvailabilityRequirementSyntax').filter(leaf => !audiences.has(leaf.audience))) {
            context.warning(DiagnosticCodes.UnknownAudience, `Unknown audience '${audience.audience}' - declare it with 'audience ${audience.audience}'`, audience.location);
        }
        for (const reference of availabilityLeaves(requirement, 'AvailabilityReferenceSyntax').filter(leaf => !availabilities.has(leaf.availability))) {
            context.warning(DiagnosticCodes.UnknownAvailability, `Unknown availability '${reference.availability}' - declare it with 'availability ${reference.availability}'`, reference.location);
        }
    }
}

function reportCycles(availabilities: readonly AvailabilitySyntax[], known: ReadonlySet<string>, context: ParserContext): void {
    const edges = new Map<string, string[]>();
    for (const availability of availabilities) {
        const targets = edges.get(availability.name) ?? [];
        for (const reference of availability.requirements.flatMap(requirement => availabilityLeaves(requirement, 'AvailabilityReferenceSyntax'))) {
            if (known.has(reference.availability) && !targets.includes(reference.availability)) targets.push(reference.availability);
        }
        edges.set(availability.name, targets);
    }
    const reported = new Set<string>();
    for (const availability of availabilities) {
        const path: string[] = [];
        if (!reported.has(availability.name) && reaches(availability.name, availability.name, edges, new Set(), path)) {
            path.forEach(name => reported.add(name));
            context.error(
                DiagnosticCodes.AvailabilityCycle,
                `Availability '${availability.name}' builds on itself through ${[...path, availability.name].join(' -> ')} - break the cycle`,
                availability.location);
        }
    }
}

function reaches(start: string, current: string, edges: ReadonlyMap<string, string[]>, visited: Set<string>, path: string[]): boolean {
    path.push(current);
    for (const next of edges.get(current) ?? []) {
        if (next === start) return true;
        if (!visited.has(next)) {
            visited.add(next);
            if (reaches(start, next, edges, visited, path)) return true;
        }
    }
    path.pop();
    return false;
}

// Warns about an invoker that is available while the command it invokes can be unavailable. The port of the C# AvailabilityInvocationValidator.
export function validateAvailabilityInvocations(application: ApplicationSyntax, context: ParserContext): void {
    const blocks = new Map<string, AvailabilitySyntax>();
    for (const availability of application.availabilities ?? []) if (!blocks.has(availability.name)) blocks.set(availability.name, availability);
    const collection = new Collection();
    for (const module of application.modules) collection.module(module);
    for (const invocation of collection.invocations) {
        const target = collection.commands.get(invocation.command);
        if (target === undefined || target.length === 0) continue;
        if (canBeOffWhileOn(invocation.gates, target, blocks)) {
            context.warning(
                DiagnosticCodes.AvailabilityHidesInvokedCommand,
                `${invocation.kind} invokes command '${invocation.command}', which can be unavailable while this ${invocation.kind} is available - give it the same 'available' gate, or accept that the invocation is refused for some callers`,
                invocation.location);
        }
    }
}

function canBeOffWhileOn(invoker: readonly AvailableSyntax[], target: readonly AvailableSyntax[], blocks: Blocks): boolean {
    const audiences = [...new Set([...invoker, ...target].flatMap(gate => audiencesOf(gate.requirement, blocks, new Set())))];
    if (audiences.length === 0 || audiences.length > maximumAudiences) return false;
    for (let mask = 0; mask < 1 << audiences.length; mask++) {
        const on = new Set(audiences.filter((_, index) => (mask & (1 << index)) !== 0));
        if (invoker.every(gate => holds(gate.requirement, on, blocks, [])) && !target.every(gate => holds(gate.requirement, on, blocks, []))) return true;
    }
    return false;
}

function audiencesOf(requirement: AvailabilityRequirementSyntax, blocks: Blocks, expanding: Set<string>): string[] {
    switch (requirement.kind) {
        case 'AudienceAvailabilityRequirementSyntax':
            return [requirement.audience];
        case 'LogicalAvailabilityRequirementSyntax':
            return [...audiencesOf(requirement.left, blocks, expanding), ...audiencesOf(requirement.right, blocks, expanding)];
        case 'AvailabilityReferenceSyntax': {
            const block = blocks.get(requirement.availability);
            if (block === undefined || expanding.has(requirement.availability)) return [];
            expanding.add(requirement.availability);
            return block.requirements.flatMap(inner => audiencesOf(inner, blocks, expanding));
        }
        default:
            return [];
    }
}

function holds(requirement: AvailabilityRequirementSyntax, on: ReadonlySet<string>, blocks: Blocks, expanding: readonly string[]): boolean {
    switch (requirement.kind) {
        case 'AudienceAvailabilityRequirementSyntax':
            return on.has(requirement.audience);
        case 'LogicalAvailabilityRequirementSyntax':
            return requirement.operator === 'And'
                ? holds(requirement.left, on, blocks, expanding) && holds(requirement.right, on, blocks, expanding)
                : holds(requirement.left, on, blocks, expanding) || holds(requirement.right, on, blocks, expanding);
        case 'AvailabilityReferenceSyntax': {
            const block = blocks.get(requirement.availability);
            // A reference nothing declares or that builds on itself is skipped, like a condition.
            if (block === undefined || expanding.includes(requirement.availability)) return true;
            return block.requirements.every(inner => holds(inner, on, blocks, [...expanding, requirement.availability]));
        }
        default:
            // A condition is opaque and skipped.
            return true;
    }
}

interface Invocation {
    readonly kind: string;
    readonly command: string;
    readonly gates: readonly AvailableSyntax[];
    readonly location: SourceLocation;
}

class Collection {
    readonly commands = new Map<string, readonly AvailableSyntax[]>();
    readonly invocations: Invocation[] = [];

    module(module: ModuleSyntax): void {
        const gates = module.available ?? [];
        for (const feature of module.features) this.#feature(feature, gates);
    }

    #feature(feature: FeatureSyntax, inherited: readonly AvailableSyntax[]): void {
        const gates = [...inherited, ...(feature.available ?? [])];
        for (const slice of feature.slices) this.#slice(slice, gates);
        for (const nested of feature.features) this.#feature(nested, gates);
    }

    #slice(slice: SliceSyntax, inherited: readonly AvailableSyntax[]): void {
        const gates = [...inherited, ...(slice.available ?? [])];
        for (const command of slice.commands) {
            if (!this.commands.has(command.name)) this.commands.set(command.name, [...gates, ...(command.available ?? [])]);
        }
        for (const screen of slice.screens) {
            const screenGates = [...gates, ...(screen.available ?? [])];
            const invocations = this.invocations;
            new class extends ScreenplaySyntaxWalker {
                override visitNode(node: SyntaxNode): void {
                    if (node.kind === 'ScreenActionSyntax') {
                        invocations.push({ kind: 'screen action', command: (node as unknown as { readonly command: string }).command, gates: screenGates, location: node.location });
                    }
                }
            }().visitScreen(screen);
        }
        for (const invokes of slice.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.invokes ?? [])) {
            this.invocations.push({ kind: 'reaction', command: invokes.command, gates: [], location: invokes.location });
        }
    }
}
