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
        schema: schemas.forProperties(command.properties),
        stateSchema: {},
        logicDescription: command.description ?? '',
        rules: rulesOf(command),
    };
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
