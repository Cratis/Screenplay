// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax, ValidationRuleSyntax } from '@cratis/screenplay-compiler';
import { CommandItemDocument, CommandRuleDocument } from '../Document/EventModelDocument';
import { SchemaSynthesizer } from '../Schemas/SchemaSynthesizer';
import { SliceScope } from './SliceScope';

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

export function toCommand(command: CommandSyntax, scope: SliceScope, schemas: SchemaSynthesizer): CommandItemDocument {
    return {
        id: scope.idOf('command', command.name),
        name: command.name,
        schema: schemas.forProperties(command.properties.filter(property => !property.isGenerated)),
        stateSchema: {},
        logicDescription: detailsOf(command),
        rules: rulesOf(command),
    };
}

function detailsOf(command: CommandSyntax): string {
    const generated = command.properties.filter(property => property.isGenerated);
    if (generated.length === 0 && command.response == null) return command.description ?? '';
    const sections = [command.description ?? '', 'Syntax-only: execution unavailable until ESM v8 (PLAY0268).'];
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
