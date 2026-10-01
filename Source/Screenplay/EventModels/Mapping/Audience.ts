// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizeSyntax, PersonaSyntax, PolicyRequirementSyntax, SliceSyntax } from '@cratis/screenplay-compiler';
import { ActorDocument, ActorType } from '../Document/EventModelDocument';
import { guidFor } from '../Document/identity';
import { userActor } from '../Prototypes/toUserExperience';

// Who uses a slice's screens. A persona does when the policies it holds satisfy everything the slice is
// gated by - its module's, its features' and its own command's or query's authorize together. A slice
// gated by nothing, or by something no persona satisfies, is used by the generic user.
export class Audience {
    // The actors given a screen, shared by every narrowed audience so the rows can be drawn afterwards.
    private constructor(
        private readonly personas: readonly PersonaActor[],
        private readonly gates: readonly PolicyRequirementSyntax[],
        private readonly used = new Set<string>()) {}

    static of(personas: readonly PersonaSyntax[]): Audience {
        return new Audience(personas.filter(persona => persona.name.length > 0).map(toPersonaActor), []);
    }

    // The audience inside a module or feature, which narrows it to those its authorize lets in.
    within(authorize: AuthorizeSyntax | null): Audience {
        return authorize === null ? this : new Audience(this.personas, [...this.gates, authorize.requirement], this.used);
    }

    // The actors whose rows show the slice's screens.
    actorsFor(slice: SliceSyntax): string[] {
        const own = (slice.type === 'StateChange' ? slice.commands[0]?.authorize : slice.queries[0]?.authorize) ?? null;
        const gates = own === null ? this.gates : [...this.gates, own.requirement];
        const permitted = gates.length === 0 ? [] : this.personas.filter(persona => gates.every(gate => satisfies(gate, persona.policies)));
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
}

interface PersonaActor {
    readonly actor: ActorDocument;
    readonly policies: ReadonlySet<string>;
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
