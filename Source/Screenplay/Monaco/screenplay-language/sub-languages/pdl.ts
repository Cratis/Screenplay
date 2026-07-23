// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SubLanguageDefinition } from '../sub-language-registry';

// Projection Declaration Language (PDL) — embedded inside `projection` blocks.
// See https://www.cratis.io/chronicle/projections/projection-declaration-language/grammar/
export const pdl: SubLanguageDefinition = {
    tokens: [
        [
            /\b(?:from|every|all|nested|join|children|identified|by|key|parent|with|remove|via|clear|on|sequence|automap|no|exclude|literal|set|increment|decrement|add|subtract|count)\b/,
            'keyword',
        ],
    ],
    completions: [
        {
            label: 'key',
            insertText: 'key ${1:property}',
            documentation: 'Declares which property identifies the read model instance.',
        },
        {
            label: 'from',
            insertText: 'from ${1:EventType}',
            documentation: 'Maps properties from an event onto the read model.',
        },
        {
            label: 'join',
            insertText: 'join ${1:property} on ${2:key}',
            documentation: 'Joins related state onto the read model by a key property.',
        },
        {
            label: 'children',
            insertText: 'children ${1:collection} identified by ${2:key}',
            documentation: 'Projects events into a child collection on the read model.',
        },
        {
            label: 'remove with',
            insertText: 'remove with ${1:EventType}',
            documentation: 'Removes the read model instance when the event occurs.',
        },
        {
            label: 'parent',
            insertText: 'parent ${1:property}',
            documentation: 'Declares the property that identifies the parent of a child.',
        },
        {
            label: 'increment',
            insertText: 'increment ${1:property}',
            documentation: 'Increments a numeric property when the event occurs.',
        },
        {
            label: 'decrement',
            insertText: 'decrement ${1:property}',
            documentation: 'Decrements a numeric property when the event occurs.',
        },
    ],
    hovers: {
        from: 'PDL — maps properties from an event onto the read model.',
        every: 'PDL — applies property mappings for every event type in the projection.',
        join: 'PDL — joins related state onto the read model by a key property.',
        children: 'PDL — projects events into a child collection on the read model.',
        identified: 'PDL — introduces the identifying key of a child collection.',
        key: 'PDL — declares which property identifies the read model instance.',
        parent: 'PDL — declares the property that identifies the parent of a child.',
        remove: 'PDL — removes the read model instance when an event occurs.',
        increment: 'PDL — increments a numeric property when the event occurs.',
        decrement: 'PDL — decrements a numeric property when the event occurs.',
    },
};
