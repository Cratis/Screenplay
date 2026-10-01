// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { Ajv2020 } from 'ajv/dist/2020.js';
import { EventModelDocument } from '../../../Document/EventModelDocument';

const schema = JSON.parse(readFileSync(createRequire(import.meta.url).resolve('@cratis/event-models/schema'), 'utf8')) as object;
const validate = new Ajv2020({ strict: false, allErrors: true }).compile(schema);

// The problems the published board schema finds in a document - none when the board can read it.
export function problems_the_board_finds_in(document: EventModelDocument): string[] {
    return validate(document) ? [] : (validate.errors ?? []).map(error => `${error.instancePath} ${error.message ?? ''}`);
}

// Every id in a document, wherever it sits.
export function ids_in(value: unknown): string[] {
    if (Array.isArray(value)) {
        return value.flatMap(ids_in);
    }
    if (value === null || typeof value !== 'object') {
        return [];
    }
    return Object.entries(value).flatMap(([member, child]) => member === 'id' && typeof child === 'string' ? [child] : ids_in(child));
}
