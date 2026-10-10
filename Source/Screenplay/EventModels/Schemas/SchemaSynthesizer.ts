// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, ConceptSyntax, PropertySyntax, TypeRefSyntax, TypeSyntax } from '@cratis/screenplay-compiler';
import { JsonSchemaObject } from '../Document/EventModelDocument';

// Turns declared properties into the JSON Schema the board shows on a card. A concept takes the shape of
// the primitive it wraps - an enumeration its values - and a declared type its own properties; a name the
// document does not declare is a string, which is how Studio draws it too.
export class SchemaSynthesizer {
    readonly #concepts: Map<string, ConceptSyntax>;
    readonly #types: Map<string, TypeSyntax>;

    constructor(application: ApplicationSyntax) {
        this.#concepts = new Map(application.concepts.map(concept => [concept.name, concept]));
        this.#types = new Map(application.types.map(type => [type.name, type]));
    }

    forProperties(properties: readonly PropertySyntax[], visiting: ReadonlySet<string> = new Set()): JsonSchemaObject {
        const declared = properties.filter(property => property.name.trim().length > 0);
        if (declared.length === 0) {
            return {};
        }
        const schema: JsonSchemaObject = {
            type: 'object',
            properties: Object.fromEntries(declared.map(property => [property.name, { ...this.forType(property.type, visiting), ...(property.isKey ? { title: `${property.name} (key)`, description: 'Read-model key part' } : {}) }])),
        };
        const required = declared.filter(property => !property.type.isOptional).map(property => property.name);
        if (required.length > 0) {
            schema.required = required;
        }
        return schema;
    }

    forType(type: TypeRefSyntax, visiting: ReadonlySet<string> = new Set()): JsonSchemaObject {
        const item = this.#named(type.name, visiting);
        return type.isCollection ? { type: 'array', items: item } : item;
    }

    #named(name: string, visiting: ReadonlySet<string>): JsonSchemaObject {
        const concept = this.#concepts.get(name);
        if (concept !== undefined) {
            return concept.type === 'Enum' ? { type: 'string', enum: [...concept.values] } : scalar(concept.type);
        }
        const declared = this.#types.get(name);
        if (declared !== undefined && !visiting.has(name)) {
            return this.forProperties(declared.properties, new Set([...visiting, name]));
        }
        return scalar(name);
    }
}

function scalar(name: string): JsonSchemaObject {
    switch (name.toLowerCase()) {
        case 'int': case 'integer': case 'int32': case 'int64': case 'long': case 'short':
            return { type: 'integer' };
        case 'decimal': case 'number': case 'double': case 'float':
            return { type: 'number' };
        case 'bool': case 'boolean':
            return { type: 'boolean' };
        case 'date':
            return { type: 'string', format: 'date' };
        case 'datetime': case 'date-time': case 'timestamp':
            return { type: 'string', format: 'date-time' };
        case 'uuid': case 'guid':
            return { type: 'string', format: 'uuid' };
        default:
            return { type: 'string' };
    }
}
