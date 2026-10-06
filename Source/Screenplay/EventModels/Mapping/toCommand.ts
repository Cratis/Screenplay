// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionKind, OperationPhaseSyntax, OperationSyntax, CommandSyntax, ValidationRuleSyntax } from '@cratis/screenplay-compiler';
import { CommandItemDocument, CommandRuleDocument } from '../Document/EventModelDocument';
import { SchemaSynthesizer } from '../Schemas/SchemaSynthesizer';
import { SliceScope } from './SliceScope';
import { EventOwners } from './EventOwners';
import { expressionText } from './expressionText';

// The rule kinds the board has an equivalent for, by the name it shows. Equal, NotEqual, the element-wise
// rules and named rules have none and are left out rather than mapped onto their nearest neighbor - the
// same choice Studio makes. A rule whose operand is not a literal is left out too: it names something
// resolved while the application runs, and a board rule holds a fixed value.
const ruleTypes: Partial<Record<ValidationRuleSyntax['rule'], string>> = {
    NotEmpty: 'NotEmpty',
    Min: 'MinLength',
    Max: 'MaxLength',
    Length: 'Length',
    GreaterThan: 'GreaterThan',
    GreaterThanOrEqual: 'GreaterThanOrEqual',
    LessThan: 'LessThan',
    LessThanOrEqual: 'LessThanOrEqual',
    Matches: 'Matches',
};

export function toCommand(command: CommandSyntax, scope: SliceScope, schemas: SchemaSynthesizer, owners?: EventOwners): CommandItemDocument {
    return {
        id: scope.idOf('command', command.name),
        name: command.name,
        schema: schemas.forProperties(command.properties.filter(property => !property.isGenerated)),
        stateSchema: {},
        logicDescription: [detailsOf(command), routeDetails(command), operationDetails(command, owners)].filter(Boolean).join('\n\n'),
        rules: rulesOf(command),
    };
}

function operationDetails(command: CommandSyntax, owners?: EventOwners): string {
    const slice = owners?.productions?.slices.find(entry => entry.slice.commands.includes(command))?.slice;
    if (!slice || !owners?.productions) return '';
    const productions = command.produces.map((production, index) => ({ production, index, resolution: owners.productions!.resolve(production.event, slice) }));
    const operations = productions.filter(entry => entry.production.inlineOperation != null || entry.resolution.kind === AuthoringProductionKind.Operation ||
        entry.resolution.candidates.some(candidate => candidate.kind === AuthoringProductionKind.Operation));
    const specs = slice.specifications.filter(specification => specification.when?.commandType === command.name)
        .flatMap(specification => [
            ...(specification.givenOperationFailures ?? []).map(step => `${specification.name}: given operation ${step.operation} fails`),
            ...(specification.thenOperations ?? []).map(step => `${specification.name}: then operation ${step.operation}${step.values.map(value => `\n  ${value.property} = ${expressionText(value.source)}`).join('')}`),
            ...(specification.thenCompensated ?? []).map(step => `${specification.name}: then compensated ${step.operation}`)
        ]);
    if (operations.length === 0 && specs.length === 0) return '';
    const phase = (name: string, value: OperationPhaseSyntax | null) => `${name}: ${!value ? 'not declared' : value.file ? `file ${value.file.path}` : value.code ? `inline ${value.code.language}` : 'pending'}${value?.description ? ` — ${value.description}` : ''}${value?.implementation?.hints.map(hint => `\n  hint: ${hint.text}`).join('') ?? ''}`;
    return ['Syntax-only operation intent: not admitted by any supported executable model (ESM) version yet (PLAY0268) (#301).',
        'Authored productions\n' + productions.map(entry => `${entry.index + 1}. ${entry.resolution.kind[0].toUpperCase() + entry.resolution.kind.slice(1)}: ${entry.production.event}`).join('\n'),
        ...operations.map(entry => {
            const operation = entry.production.inlineOperation ?? (entry.resolution.kind === AuthoringProductionKind.Operation ? entry.resolution.declaration?.node as OperationSyntax : undefined);
            if (!operation) return `${entry.production.event}: ambiguous declaration kind; no operation selected.`;
            const systems = owners.systems.filter(system => system.name === operation.uses);
            return [`Operation ${operation.name}`, operation.description ?? '', `Uses ${operation.uses}${systems.length === 1 && systems[0].description ? ` — ${systems[0].description}` : ''}`,
                ...operation.inputs.map(input => `${input.name}: ${input.type.name}${input.type.isCollection ? '[]' : ''}${input.type.isOptional ? ' optional' : ''}`),
                ...entry.production.mappings.map(mapping => `${mapping.property} = ${expressionText(mapping.source)}`),
                phase('execute', operation.execute), phase('compensate', operation.compensate)].filter(Boolean).join('\n');
        }), ...specs].join('\n\n').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function routeDetails(command: CommandSyntax): string {
    const escape = (text: string) => text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    if ((command.streamCandidates ?? []).some(candidate => candidate.propertyCandidate != null)) return 'Ambiguous stream/property authoring: no route selected (PLAY0505). Not admitted by any supported executable model (ESM) version yet (PLAY0268) (#302).';
    if ((command.streamCandidates ?? []).length > 0) return 'Conflicting authored stream routes: no effective route selected. Not admitted by any supported executable model (ESM) version yet (PLAY0268) (#302).';
    const route = command.stream;
    if (!route) return '';
    return escape([`Authored stream: ${route.eventSource}.${route.stream}`,
        route.streamId ? `Stream id: ${expressionText(route.streamId.source)}` : '',
        'Syntax-only: not admitted by any supported executable model (ESM) version yet (PLAY0268) (#302). This classification does not supply an identity destination.'
    ].filter(Boolean).join('\n'));
}

function detailsOf(command: CommandSyntax): string {
    const generated = command.properties.filter(property => property.isGenerated);
    if (generated.length === 0 && command.response == null) return command.description ?? '';
    const sections = [command.description ?? '', 'Generated values and responses: executable as ESM v7. Generated values require fixtures in reference execution; other unadmitted constructs still prevent binding.'];
    if (generated.length > 0) sections.push(`Generated values (not request inputs)\n${generated.map(property => `${property.name}: ${property.type.name}${property.isIdentifier ? ' (identifier)' : ''}`).join('\n')}`);
    const response = command.response;
    if (response?.kind === 'ScalarCommandResponseSyntax') sections.push(`Returns\n${response.source.property}`);
    if (response?.kind === 'RecordCommandResponseSyntax') {
        const properties = new Map(command.properties.map(property => [property.name, property]));
        sections.push(`Returns\n${response.fields.map(field => {
            const type = field.type ?? properties.get(field.source.property)?.type;
            return `${field.name}${type ? `: ${type.name}${type.isOptional ? ' optional' : ''}` : ''} = ${field.source.property}`;
        }).join('\n')}`);
    }
    return sections.filter(Boolean).join('\n\n');
}

function rulesOf(command: CommandSyntax): CommandItemDocument['rules'] {
    const byProperty = new Map<string, CommandRuleDocument[]>();
    for (const validate of command.validations) {
        if (validate.kind !== 'DeclarativeValidateSyntax') {
            continue;
        }
        for (const rule of validate.rules) {
            const carried = toRule(rule);
            if (carried !== undefined && rule.property.trim().length > 0) {
                byProperty.set(rule.property, [...(byProperty.get(rule.property) ?? []), carried]);
            }
        }
    }
    return [...byProperty].map(([propertyName, rules]) => ({ propertyName, rules }));
}

function toRule(rule: ValidationRuleSyntax): CommandRuleDocument | undefined {
    const ruleType = ruleTypes[rule.rule];
    if (ruleType === undefined || (rule.rule !== 'NotEmpty' && rule.value?.kind !== 'LiteralExpressionSyntax')) {
        return undefined;
    }
    return { errorMessage: rule.message ?? '', ruleType };
}
