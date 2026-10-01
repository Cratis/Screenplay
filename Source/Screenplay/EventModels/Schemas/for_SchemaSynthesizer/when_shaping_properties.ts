// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { JsonSchemaObject } from '../../Document/EventModelDocument';
import { SchemaSynthesizer } from '../SchemaSynthesizer';

describe('when shaping properties', () => {
    let schema: JsonSchemaObject;

    beforeAll(() => {
        const application = parse([
            'concept Status : Enum',
            '  active',
            '  closed',
            'concept Amount : Decimal',
            'type Node',
            '  name String',
            '  next Node?',
            'type Shape',
            '  status Status',
            '  amount Amount',
            '  node Node',
            '  when DateTime',
            '  day Date',
            '  flag Bool',
            '  count Int',
            '  key Guid',
            '  tags String[]?',
            '  other Unknown',
        ].join('\n')).value;
        schema = new SchemaSynthesizer(application).forProperties(application.types[1].properties);
    });

    it('should require only what is not optional', () => {
        (schema.required as string[]).should.deep.equal(['status', 'amount', 'node', 'when', 'day', 'flag', 'count', 'key', 'other']);
    });

    it('should shape every kind of type', () => {
        (schema.properties as Record<string, unknown>).should.deep.equal({
            status: { type: 'string', enum: ['active', 'closed'] },
            amount: { type: 'number' },
            node: {
                type: 'object',
                properties: { name: { type: 'string' }, next: { type: 'string' } },
                required: ['name'],
            },
            when: { type: 'string', format: 'date-time' },
            day: { type: 'string', format: 'date' },
            flag: { type: 'boolean' },
            count: { type: 'integer' },
            key: { type: 'string', format: 'uuid' },
            tags: { type: 'array', items: { type: 'string' } },
            other: { type: 'string' },
        });
    });
});
