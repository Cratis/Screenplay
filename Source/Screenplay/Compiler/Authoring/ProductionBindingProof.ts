// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLine } from '../Parsing/SourceLine';
import { CommandSyntax } from '../Syntax/Commands';
import { ConceptSyntax, EventSyntax, PropertySyntax } from '../Syntax/Declarations';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax, SliceSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const primitives = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime']);
const admitted = new Set(['ApplicationSyntax', 'ModuleSyntax', 'FeatureSyntax', 'SliceSyntax', 'CommandSyntax', 'EventSyntax', 'ConceptSyntax', 'PropertySyntax', 'TypeRefSyntax', 'ProducesSyntax', 'PropertyMappingSyntax', 'PathExpressionSyntax', 'ContextExpressionSyntax']);
const unique = (values: readonly { name: string }[]) => new Set(values.map(value => value.name)).size === values.length;

// The TS projection deliberately omits executable constructs. Do not mistake a successful parse for
// a bindable model. Prove a small, closed command/event subset, and leave everything else to the C#
// repair transaction. Every significant source line must belong to an admitted modeled node: opaque
// handlers, requirements, metadata and conditional bodies cannot silently slip through this whitelist.
export class ProductionBindingProof extends ScreenplaySyntaxWalker {
    private valid = true;
    private readonly committedLines = new Set<number>();
    private readonly types = new Set(primitives);
    private readonly events = new Map<string, { declaration: EventSyntax; properties: ReadonlyMap<string, PropertySyntax>; required: number }>();
    private readonly commands: CommandSyntax[] = [];

    constructor(application: ApplicationSyntax) {
        super();
        this.valid = unique(application.modules);
        this.visitApplication(application);
    }

    permits(lines: readonly SourceLine[]): boolean {
        return this.valid && lines.every(line => line.content.length === 0 || this.committedLines.has(line.number)) &&
            [...this.events.values()].every(event => this.propertiesAreKnown(event.declaration.properties)) &&
            this.commands.every(command => this.commandBinds(command));
    }

    override visitNode(node: SyntaxNode): void {
        if (!admitted.has(node.kind)) this.valid = false;
        if (node.kind !== 'ApplicationSyntax') this.committedLines.add(node.location.line);
    }

    override visitConcept(concept: ConceptSyntax): void {
        if (this.types.has(concept.name) || !primitives.has(concept.type) || concept.attributes.length > 0 || concept.values.length > 0) this.valid = false;
        this.types.add(concept.name);
        super.visitConcept(concept);
    }

    override visitModule(module: ModuleSyntax): void {
        if (!unique(module.features)) this.valid = false;
        super.visitModule(module);
    }

    override visitFeature(feature: FeatureSyntax): void {
        if (!unique(feature.features) || !unique(feature.slices)) this.valid = false;
        super.visitFeature(feature);
    }

    override visitSlice(slice: SliceSyntax): void {
        if (slice.type !== 'StateChange' || !unique(slice.commands)) this.valid = false;
        super.visitSlice(slice);
    }

    override visitEvent(event: EventSyntax): void {
        if (this.events.has(event.name) || event.hasGenerationMarker || event.id !== null) this.valid = false;
        this.events.set(event.name, { declaration: event, properties: new Map(event.properties.map(property => [property.name, property])), required: event.properties.filter(property => !property.type.isOptional).length });
        super.visitEvent(event);
    }

    override visitCommand(command: CommandSyntax): void {
        this.commands.push(command);
        super.visitCommand(command);
    }

    private propertiesAreKnown(properties: readonly PropertySyntax[]): boolean {
        return unique(properties) && properties.every(property => this.types.has(property.type.name));
    }

    private commandBinds(command: CommandSyntax): boolean {
        if (!this.propertiesAreKnown(command.properties)) return false;
        const properties = new Map(command.properties.map(property => [property.name, property]));
        const identifiers = command.properties.filter(property => property.isIdentifier);
        if (identifiers.length > 1) return false;
        const identifier = identifiers[0];
        for (const production of command.produces) {
            const event = this.events.get(production.event);
            if (event === undefined) return false;
            if (production.for !== null || production.inlineEvent !== null) {
                if (identifier === undefined || identifier.type.isCollection || identifier.type.isOptional ||
                    (production.for !== null && (production.for.kind !== 'PathExpressionSyntax' || production.for.path !== identifier.name))) return false;
            }
            const mapped = new Set<string>();
            let required = 0;
            for (const mapping of production.mappings) {
                const target = event.properties.get(mapping.property);
                if (target === undefined || mapped.has(mapping.property) || !this.mappingBinds(mapping, target, properties)) return false;
                mapped.add(mapping.property);
                if (!target.type.isOptional) required++;
            }
            if (required !== event.required) return false;
        }
        return true;
    }

    private mappingBinds(mapping: PropertyMappingSyntax, target: PropertySyntax, properties: ReadonlyMap<string, PropertySyntax>): boolean {
        if (mapping.source.kind === 'ContextExpressionSyntax') {
            return mapping.source.path === 'occurred' && target.type.name === 'DateTime' && !target.type.isCollection && !target.type.isOptional;
        }
        if (mapping.source.kind !== 'PathExpressionSyntax') return false;
        const source = properties.get(mapping.source.path);
        return source !== undefined && source.type.name === target.type.name && source.type.isCollection === target.type.isCollection && source.type.isOptional === target.type.isOptional;
    }
}
