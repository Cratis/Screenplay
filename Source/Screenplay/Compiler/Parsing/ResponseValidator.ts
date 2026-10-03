// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CommandSyntax } from '../Syntax/Commands';
import { ConceptSyntax, PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { PropertyResponseSourceSyntax } from '../Syntax/Responses';
import { SpecificationSyntax } from '../Syntax/Specifications';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';
import { InputUse } from './InputUses';
import { ParserContext } from './ParserContext';

const primitiveTypes = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime']);
interface ScopedCommand { command: CommandSyntax; scope: readonly string[] }

export function validateResponses(application: ApplicationSyntax, context: ParserContext, inputs: readonly InputUse[] = context.inputUses): void {
    const slices: { slice: SliceSyntax; scope: readonly string[] }[] = [];
    const collect = (feature: FeatureSyntax, parent: readonly string[]): void => {
        const scope = [...parent, feature.name];
        feature.slices.forEach(slice => slices.push({ slice, scope: [...scope, slice.name] }));
        feature.features.forEach(child => collect(child, scope));
    };
    application.modules.forEach(module => module.features.forEach(feature => collect(feature, [module.name])));
    const commands = new Map<string, ScopedCommand[]>();
    const properties = new Map<CommandSyntax, Map<string, PropertySyntax>>();
    for (const { slice, scope } of slices) {
        for (const command of slice.commands) {
            const entries = commands.get(command.name) ?? [];
            entries.push({ command, scope });
            commands.set(command.name, entries);
            properties.set(command, uniqueByName(command.properties));
        }
    }
    const imports = new Map<string, string[]>();
    for (const imported of application.imports) {
        const name = imported.qualifiedName.split('.').at(-1)!;
        imports.set(name, [...(imports.get(name) ?? []), imported.qualifiedName]);
    }
    const resolved = new Map<string, CommandSyntax | null>();
    const resolve = (name: string, from: readonly string[]): CommandSyntax | null => {
        const key = `${from.join('.')}|${name}`;
        if (resolved.has(key)) return resolved.get(key)!;
        const imported = imports.get(name) ?? [];
        const local = commands.get(name.split('.').at(-1)!) ?? [];
        const reference = local.length === 0 && imported.length === 1 ? imported[0] : name;
        const segments = reference.split('.');
        let candidates = commands.get(segments.at(-1)!) ?? [];
        if (segments.length > 1) {
            const qualifiers = segments.slice(0, -1);
            candidates = candidates.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((value, index) => entry.scope[entry.scope.length - qualifiers.length + index] === value));
        } else {
            for (let depth = from.length; depth >= 0; depth--) {
                const visible = candidates.filter(entry => depth <= entry.scope.length && from.slice(0, depth).every((value, index) => entry.scope[index] === value));
                if (visible.length > 0) { candidates = visible; break; }
            }
        }
        const command = candidates.length === 1 ? candidates[0].command : null;
        resolved.set(key, command);
        return command;
    };
    const concepts = uniqueByName(application.concepts);
    const conceptNames = new Set(application.concepts.map(concept => concept.name));
    const composites = new Map([...uniqueByName(application.types)].map(([name, type]) => [name, uniqueByName(type.properties)]));
    const readModels = new Set(slices.flatMap(entry => entry.slice.readModels.map(model => model.name)));
    const commandProperties = new Set([...properties.keys()].flatMap(command => [...command.properties]));
    const checkDeclarations = (declared: readonly PropertySyntax[]): void => {
        for (const property of declared) {
            if (property.isGenerated && !commandProperties.has(property)) context.error(DiagnosticCodes.GeneratedPropertyOutsideCommand, 'Generated properties can only be declared on commands.', property.location);
        }
    };
    // Only these typed declarations carry PropertySyntax. Do not pull the complete extensible walker
    // into every browser parser just to inspect their generated flags.
    const checkFeature = (feature: FeatureSyntax): void => {
        feature.features.forEach(checkFeature);
        for (const slice of feature.slices) {
            for (const command of slice.commands) {
                for (const production of command.produces) if (production.inlineEvent !== null) checkDeclarations(production.inlineEvent.properties);
            }
            slice.events.forEach(event => checkDeclarations(event.properties));
            slice.readModels.forEach(model => checkDeclarations(model.properties));
            for (const reaction of slice.reactions) {
                for (const trigger of reaction.triggers) {
                    for (const production of trigger.produces) if (production.inlineEvent !== null) checkDeclarations(production.inlineEvent.properties);
                }
            }
        }
    };
    application.types.forEach(type => checkDeclarations(type.properties));
    application.modules.forEach(module => module.features.forEach(checkFeature));
    const validateSource = (source: PropertyResponseSourceSyntax, explicit: TypeRefSyntax | null, command: CommandSyntax): void => {
        const property = properties.get(command)!.get(source.property);
        if (property === undefined) {
            context.error(DiagnosticCodes.InvalidResponseSource, `Response source '${source.property}' must reference one direct command property.`, source.location);
            return;
        }
        const type = property.type;
        if (type.isCollection || readModels.has(type.name)) context.error(DiagnosticCodes.InvalidResponseShape, 'Collection and whole-read-model responses are not supported.', source.location);
        if (explicit !== null && (explicit.name !== type.name || explicit.isCollection !== type.isCollection || explicit.isOptional !== type.isOptional)) context.error(DiagnosticCodes.InvalidResponseShape, "An explicit response field type must match its source's declared type, collection shape and optionality.", explicit.location);
    };
    for (const command of properties.keys()) {
        for (const property of command.properties) {
            if (!property.isGenerated) continue;
            const concept = concepts.get(property.type.name);
            if (property.type.isOptional || property.type.isCollection || (concept === undefined ? primitiveTypes.has(property.type.name) || composites.has(property.type.name) || conceptNames.has(property.type.name) || readModels.has(property.type.name) : concept.type !== 'Uuid')) context.error(DiagnosticCodes.InvalidGeneratedType, 'A generated property must be a required, noncollection concept backed by Uuid.', property.location);
        }
        const response = command.response;
        if (response?.kind === 'ScalarCommandResponseSyntax') validateSource(response.source, null, command);
        else if (response?.kind === 'RecordCommandResponseSyntax') {
            if (response.fields.length === 0) context.error(DiagnosticCodes.InvalidCommandResponse, 'A response block requires at least one field.', response.location);
            const names = new Set<string>();
            for (const field of response.fields) {
                if (names.has(field.name)) context.error(DiagnosticCodes.DuplicateResponseField, `Duplicate response field '${field.name}'.`, field.location);
                names.add(field.name);
                validateSource(field.source, field.type, command);
            }
        }
    }
    const validateSpecification = (specification: SpecificationSyntax, scope: readonly string[]): void => {
        const action = specification.when;
        const command = action === null ? null : resolve(action.commandType, scope);
        if (action !== null && command !== null) {
            const declared = properties.get(command)!;
            for (const input of action.values) {
                if (declared.get(input.property.split('.')[0])?.isGenerated) context.error(DiagnosticCodes.GeneratedPropertySuppliedAsInput, `Generated property '${input.property}' cannot be supplied as request or form input.`, input.location);
            }
            const names = new Set<string>();
            for (const fixture of action.generatedValues ?? []) {
                const property = declared.get(fixture.property);
                if (names.has(fixture.property) || property === undefined || !property.isGenerated || property.isIdentifier || !compatibleValue(fixture.source, property.type, concepts, composites)) context.error(DiagnosticCodes.InvalidGeneratedFixture, 'A generated fixture must uniquely supply a compatible value for a nonidentifier generated command property.', fixture.location);
                names.add(fixture.property);
            }
        }
        const expectation = specification.thenReturns;
        if (expectation == null) return;
        if (action === null || specification.thenErrors.length > 0 || specification.thenDenied != null) context.error(DiagnosticCodes.InvalidReturnExpectation, 'A return expectation requires a command action and cannot accompany an error or denial.', expectation.location);
        if (command === null) return;
        const response = command.response;
        const declared = properties.get(command)!;
        if (expectation.kind === 'ScalarSpecificationReturnSyntax' && response?.kind === 'ScalarCommandResponseSyntax' && declared.has(response.source.property)) {
            if (!compatibleValue(expectation.value, declared.get(response.source.property)!.type, concepts, composites)) context.error(DiagnosticCodes.InvalidReturnExpectation, "The expected return value must match the response source's type.", expectation.location);
        } else if (expectation.kind === 'RecordSpecificationReturnSyntax' && response?.kind === 'RecordCommandResponseSyntax') {
            if (expectation.fields.length === 0) context.error(DiagnosticCodes.InvalidReturnExpectation, 'A record return expectation requires at least one field.', expectation.location);
            const fields = uniqueByName(response.fields);
            const asserted = new Set<string>();
            for (const field of expectation.fields) {
                const contract = fields.get(field.property);
                const property = contract === undefined ? undefined : declared.get(contract.source.property);
                if (asserted.has(field.property) || property === undefined || !compatibleValue(field.source, property.type, concepts, composites)) context.error(DiagnosticCodes.InvalidReturnExpectation, 'A return assertion must uniquely name a response field and supply a compatible concrete value.', field.location);
                asserted.add(field.property);
            }
        } else {
            context.error(DiagnosticCodes.InvalidReturnExpectation, "The return expectation must match the command's scalar or record response shape.", expectation.location);
        }
    };
    for (const { slice, scope } of slices) slice.specifications.forEach(specification => validateSpecification(specification, scope));
    for (const input of inputs) {
        const command = resolve(input.command, input.scope);
        if (command !== null && properties.get(command)!.get(input.property.split('.')[0])?.isGenerated) context.error(DiagnosticCodes.GeneratedPropertySuppliedAsInput, `Generated property '${input.property}' cannot be supplied as request or form input.`, input.location);
    }
}

function uniqueByName<T extends { readonly name: string }>(items: readonly T[]): Map<string, T> {
    const result = new Map<string, T>();
    const duplicates = new Set<string>();
    for (const item of items) {
        if (result.has(item.name)) duplicates.add(item.name);
        result.set(item.name, item);
    }
    duplicates.forEach(name => result.delete(name));
    return result;
}

function compatibleValue(value: ExpressionSyntax, type: TypeRefSyntax, concepts: ReadonlyMap<string, ConceptSyntax>, composites: ReadonlyMap<string, ReadonlyMap<string, PropertySyntax>>): boolean {
    if (value.kind === 'LiteralExpressionSyntax' && value.value === null) return type.isOptional;
    if (type.isCollection) return value.kind === 'ListExpressionSyntax' && value.items.every(item => compatibleValue(item, { ...type, isCollection: false, isOptional: false }, concepts, composites));
    const properties = composites.get(type.name);
    if (properties !== undefined) return value.kind === 'ObjectExpressionSyntax' && value.members.every(member => {
        const property = properties.get(member.name);
        return property !== undefined && compatibleValue(member.value, property.type, concepts, composites);
    });
    const concept = concepts.get(type.name);
    const primitive = concept?.type ?? type.name;
    if (primitive === 'Enum') return value.kind === 'LiteralExpressionSyntax' && typeof value.value === 'string' && concept!.values.includes(value.value);
    if (value.kind === 'LiteralExpressionSyntax') {
        switch (primitive) {
            case 'Uuid': return typeof value.value === 'string' && uuidValue(value.value);
            case 'String': return typeof value.value === 'string';
            case 'Bool': return typeof value.value === 'boolean';
            case 'Int': return typeof value.value === 'number' && Number.isFinite(value.value) && Number.isInteger(value.value);
            case 'Decimal': return typeof value.value === 'number' && Number.isFinite(value.value);
            case 'Date': return typeof value.value === 'string' && dateValue(value.value, false);
            case 'DateTime': return typeof value.value === 'string' && dateValue(value.value, true);
            default: return !primitiveTypes.has(primitive);
        }
    }
    return !primitiveTypes.has(primitive) && concept === undefined;
}

function uuidValue(text: string): boolean {
    const trimmed = text.trim();
    const braces = trimmed.startsWith('{') && trimmed.endsWith('}');
    const parentheses = trimmed.startsWith('(') && trimmed.endsWith(')');
    const value = braces || parentheses ? trimmed.slice(1, -1).trim() : trimmed;
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) ||
        (!braces && !parentheses && /^[0-9a-f]{32}$/i.test(value)) ||
        (braces && /^0x[0-9a-f]{1,8}\s*,\s*0x[0-9a-f]{1,4}\s*,\s*0x[0-9a-f]{1,4}\s*,\s*\{\s*0x[0-9a-f]{1,2}(?:\s*,\s*0x[0-9a-f]{1,2}){7}\s*\}$/i.test(value));
}

function dateValue(text: string, includeTime: boolean): boolean {
    // JavaScript rolls invalid calendar days into the next month; .NET rejects them.
    if (!includeTime && /(?:T\d|\d:)/.test(text)) return false;
    const calendar = /^(\d{4})-(\d{2})-(\d{2})(?:$|[T\s])/.exec(text);
    if (calendar !== null) {
        const year = Number(calendar[1]);
        const month = Number(calendar[2]);
        const day = Number(calendar[3]);
        const leap = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
        const days = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        if (year < 1 || month < 1 || month > 12 || day < 1 || day > days[month - 1]) return false;
    }
    const offset = /[+-](\d{2}):(\d{2})$/.exec(text);
    if (offset !== null && (Number(offset[1]) > 14 || Number(offset[2]) > 59 || (Number(offset[1]) === 14 && Number(offset[2]) !== 0))) return false;
    return !Number.isNaN(Date.parse(text));
}
