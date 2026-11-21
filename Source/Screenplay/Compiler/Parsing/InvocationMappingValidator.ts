// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax } from '../Syntax/Declarations';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { InvokesSyntax, ReactionTriggerSyntax } from '../Syntax/Reactions';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { RefusalDeclarations } from './RefusalDeclarations';

const clocks = new Set(['now', 'today']);

// Occurrence paths are single values, not an implicit iteration over a collection.
export function validateInvocationMappings(application: ApplicationSyntax, context: ParserContext): void {
    const validator = new InvocationMappingValidator(application, context);
    validator.validate();
}

class InvocationMappingValidator {
    private readonly declarations;

    constructor(private readonly application: ApplicationSyntax, private readonly context: ParserContext) {
        this.declarations = new RefusalDeclarations(application);
    }

    validate(): void {
        for (const { slice } of this.declarations.slices) {
            for (const trigger of slice.reactions.flatMap(reaction => reaction.triggers)) this.validateTrigger(trigger, slice);
        }
    }

    private validateTrigger(trigger: ReactionTriggerSyntax, slice: SliceSyntax): void {
        const sources = this.sources(trigger, slice);
        for (const invocation of trigger.invokes ?? []) {
            for (const mapping of invocation.mappings ?? []) {
                const target = this.target(invocation, mapping, slice);
                if (target === null) continue;
                // An imported input type can still have an unknown enum or record shape.
                if (this.declarations.compatible(target.type, target.type) === null) continue;
                if (mapping.source.kind === 'PathExpressionSyntax' && !this.isOtherSource(mapping.source.path, target, trigger)) {
                    this.resolve(sources, mapping.source.path, mapping.location, 'Trigger');
                }
            }
        }
    }

    private target(invocation: InvokesSyntax, mapping: PropertyMappingSyntax, slice: SliceSyntax): PropertySyntax | null {
        if (invocation.command.includes('.')) {
            const ability = (this.application.systems ?? []).flatMap(system => system.abilities.filter(ability => invocation.command === `${system.name}.${ability.name}`))[0];
            // Ability validation already owns unknown input diagnostics.
            return this.declarations.property(ability?.inputs ?? null, mapping.property);
        }
        const command = this.declarations.resolve(invocation.command, slice, owner => owner.commands)?.node;
        return this.resolve(command?.properties ?? null, mapping.property, mapping.location, `Command '${invocation.command}'`);
    }

    private sources(trigger: ReactionTriggerSyntax, slice: SliceSyntax): readonly PropertySyntax[] | null {
        const source = trigger.source;
        if (source.kind !== 'NamedTriggerSourceSyntax') return [];
        const occurrence = this.declarations.event(source.name, slice);
        if (occurrence !== null) return occurrence.properties;
        const declared = this.application.declaredTriggers?.filter(candidate => candidate.name === source.name) ?? [];
        if (declared.length !== 1) return null;
        return [...declared[0].data, ...(declared[0].untypedValues ?? []).map(name => this.untypedSource(name, source.location))];
    }

    private untypedSource(name: string, location: SourceLocation): PropertySyntax {
        return { kind: 'PropertySyntax', name, type: { kind: 'TypeRefSyntax', name: '', isCollection: false, isOptional: false, location }, isIdentifier: false, location };
    }

    private isOtherSource(path: string, target: PropertySyntax | null, trigger: ReactionTriggerSyntax): boolean {
        return this.isIntrinsic(path) || this.isRead(path, trigger) || this.isEnumeration(path, target);
    }

    private isIntrinsic(path: string): boolean {
        return path.startsWith('$') || clocks.has(path);
    }

    private isRead(path: string, trigger: ReactionTriggerSyntax): boolean {
        const root = path.split('.')[0];
        return (dependencySourcesOf(trigger).reads ?? []).some(read => (read.alias ?? read.readModel) === root);
    }

    private isEnumeration(path: string, target: PropertySyntax | null): boolean {
        const concept = target === null ? undefined : this.declarations.concepts.get(target.type.name);
        if (concept?.type !== 'Enum') return false;
        return concept.values.includes(path) || concept.values.some(member => path === `${concept.name}.${member}`);
    }

    private resolve(properties: readonly PropertySyntax[] | null, path: string, location: SourceLocation, owner: string): PropertySyntax | null {
        if (properties === null) return null;
        const segments = path.split('.');
        let property: PropertySyntax | null = null;
        for (const [index] of segments.entries()) {
            property = this.declarations.property(properties, segments.slice(0, index + 1).join('.'));
            if (property === null) {
                this.context.error(DiagnosticCodes.InvalidInvocationMapping, `${owner} declares no field '${path}'.`, location);
                return null;
            }
            if (index === segments.length - 1) return property;
            if (property.type.isCollection) {
                this.context.error(DiagnosticCodes.InvalidInvocationMapping, `${owner} path '${path}' traverses collection '${property.name}'; invocation mappings do not fan out collections.`, location);
                return null;
            }
            if (!this.hasShape(property.type.name)) return null;
        }
        return property;
    }

    private hasShape(name: string): boolean {
        return this.declarations.isScalar(name) || this.declarations.types.has(name);
    }
}
