// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizeSyntax, AvailabilityRequirementSyntax, AvailabilitySyntax, AvailableSyntax, PersonaSyntax, PolicyRequirementSyntax, SliceSyntax } from '@cratis/screenplay-compiler';
import { ActorDocument, ActorType } from '../Document/EventModelDocument';
import { guidFor } from '../Document/identity';
import { userActor } from '../Prototypes/toUserExperience';

// Who uses a slice's screens. A persona does when the policies it holds satisfy everything the slice is
// gated by - its module's, its features' and its own command's or query's authorize together. A slice
// gated by nothing, or by something no persona satisfies, is used by the generic user.
//
// 'available' gates narrow it the same way: a persona passes one when it lists the audience an 'available for'
// names, or meets the availability an 'available with' names. A condition ('available when') reads the live
// context, which the board cannot know, so it never narrows. A slice whose screens each carry a gate is drawn
// for a persona that passes any one of them; a screen without a gate passes everyone.
export class Audience {
    // The actors given a screen, shared by every narrowed audience so the rows can be drawn afterwards.
    private constructor(
        private readonly personas: readonly PersonaActor[],
        private readonly gates: readonly PolicyRequirementSyntax[],
        private readonly availabilities: ReadonlyMap<string, AvailabilitySyntax>,
        private readonly availableGates: readonly AvailabilityRequirementSyntax[],
        private readonly used = new Set<string>()) {}

    static of(personas: readonly PersonaSyntax[], availabilities: readonly AvailabilitySyntax[] = []): Audience {
        return new Audience(
            personas.filter(persona => persona.name.length > 0).map(toPersonaActor),
            [],
            new Map(availabilities.map(availability => [availability.name, availability])),
            []);
    }

    // The audience inside a module or feature, which narrows it to those its authorize and available let in.
    within(authorize: AuthorizeSyntax | null, available: readonly AvailableSyntax[] = []): Audience {
        if (authorize === null && available.length === 0) {
            return this;
        }
        return new Audience(
            this.personas,
            authorize === null ? this.gates : [...this.gates, authorize.requirement],
            this.availabilities,
            [...this.availableGates, ...available.map(gate => gate.requirement)],
            this.used);
    }

    // What the slice is gated by in words, or null when no 'available' gate applies, so the board can mark it.
    // A gate no persona satisfies says so, because the slice is then drawn for the generic user.
    gateNoteFor(slice: SliceSyntax): string | null {
        const text = [...this.availabilityGatesOf(slice), ...slice.screens.flatMap(screen => (screen.available ?? []).map(gate => gate.requirement))].map(describe);
        if (text.length === 0) {
            return null;
        }
        const gated = `Gated: available ${text.join(', ')}`;
        return this.permitted(slice).length === 0 ? `${gated}. No persona is in it, so the slice is drawn for the generic user.` : gated;
    }

    // The actors whose rows show the slice's screens.
    actorsFor(slice: SliceSyntax): string[] {
        const permitted = this.permitted(slice);
        const actors = permitted.length === 0 ? [userActor.id] : permitted.map(persona => persona.actor.id);
        if (slice.screens.length > 0) {
            actors.forEach(actor => this.used.add(actor));
        }
        return actors;
    }

    // Every persona's row, then the generic user's when a screen fell to them. No rows when there is no screen.
    actors(): ActorDocument[] {
        if (this.used.size === 0) {
            return [];
        }
        return [...this.personas.map(persona => persona.actor), ...(this.used.has(userActor.id) ? [userActor] : [])];
    }

    // The 'available' gates on the containers, the slice itself and its command or query (screens are handled apart).
    private availabilityGatesOf(slice: SliceSyntax): AvailabilityRequirementSyntax[] {
        const owner = slice.type === 'StateChange' ? slice.commands[0] : slice.queries[0];
        return [...this.availableGates, ...(slice.available ?? []).map(gate => gate.requirement), ...(owner?.available ?? []).map(gate => gate.requirement)];
    }

    // The personas that satisfy every gate on the slice - empty when nothing gates it or nobody does.
    private permitted(slice: SliceSyntax): PersonaActor[] {
        const own = (slice.type === 'StateChange' ? slice.commands[0]?.authorize : slice.queries[0]?.authorize) ?? null;
        const gates = own === null ? this.gates : [...this.gates, own.requirement];
        const available = this.availabilityGatesOf(slice);
        const screens = slice.screens.map(screen => (screen.available ?? []).map(gate => gate.requirement));
        const screensGated = screens.length > 0 && screens.every(requirements => requirements.length > 0);
        if (gates.length === 0 && available.length === 0 && !screensGated) {
            return [];
        }
        return this.personas.filter(persona =>
            gates.every(gate => satisfies(gate, persona.policies))
            && available.every(gate => this.passes(gate, persona, new Set()))
            && (!screensGated || screens.some(requirements => requirements.every(gate => this.passes(gate, persona, new Set())))));
    }

    // Whether the persona passes an 'available' requirement. A condition over the live context passes; an unknown or
    // circular availability never does.
    private passes(requirement: AvailabilityRequirementSyntax, persona: PersonaActor, visiting: ReadonlySet<string>): boolean {
        switch (requirement.kind) {
            case 'AudienceAvailabilityRequirementSyntax':
                return persona.audiences.has(requirement.audience);
            case 'ConditionAvailabilityRequirementSyntax':
                return true;
            case 'AvailabilityReferenceSyntax': {
                const availability = this.availabilities.get(requirement.availability);
                if (availability === undefined || visiting.has(availability.name)) {
                    return false;
                }
                const next = new Set(visiting).add(availability.name);
                return availability.requirements.every(inner => this.passes(inner, persona, next));
            }
            case 'LogicalAvailabilityRequirementSyntax':
                return requirement.operator === 'And'
                    ? this.passes(requirement.left, persona, visiting) && this.passes(requirement.right, persona, visiting)
                    : this.passes(requirement.left, persona, visiting) || this.passes(requirement.right, persona, visiting);
        }
    }
}

interface PersonaActor {
    readonly actor: ActorDocument;
    readonly policies: ReadonlySet<string>;
    readonly audiences: ReadonlySet<string>;
}

function toPersonaActor(persona: PersonaSyntax): PersonaActor {
    return {
        actor: {
            id: guidFor(`actor:persona:${persona.name}`),
            name: persona.name,
            actorType: ActorType.uiRole,
            description: persona.description ?? '',
            persona: { id: guidFor(`persona:${persona.name}`), name: persona.name },
        },
        policies: new Set(persona.policies),
        audiences: new Set(persona.audiences ?? []),
    };
}

function satisfies(requirement: PolicyRequirementSyntax, policies: ReadonlySet<string>): boolean {
    if (requirement.kind === 'PolicyReferenceSyntax') {
        return policies.has(requirement.name);
    }
    return requirement.operator === 'And'
        ? satisfies(requirement.left, policies) && satisfies(requirement.right, policies)
        : satisfies(requirement.left, policies) || satisfies(requirement.right, policies);
}

function describe(requirement: AvailabilityRequirementSyntax): string {
    switch (requirement.kind) {
        case 'AudienceAvailabilityRequirementSyntax':
            return `for ${requirement.audience}`;
        case 'AvailabilityReferenceSyntax':
            return `with ${requirement.availability}`;
        case 'ConditionAvailabilityRequirementSyntax':
            return 'when a context condition holds';
        case 'LogicalAvailabilityRequirementSyntax':
            return `(${describe(requirement.left)} ${requirement.operator === 'And' ? 'and' : 'or'} ${describe(requirement.right)})`;
    }
}
