// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SubLanguageDefinition } from '../sub-language-registry';

// Change Data Capture Language (CDL) — embedded inside `capture` blocks.
export const cdl: SubLanguageDefinition = {
    tokens: [
        [
            /\b(?:source|api|route|poll|key|map|translate|append|when|children|identified|by|added|removed|changed)\b/,
            'keyword',
        ],
    ],
    completions: [
        {
            label: 'source',
            insertText: 'source ${1:api}',
            documentation: 'Declares where the captured data comes from.',
        },
        {
            label: 'key',
            insertText: 'key ${1:property}',
            documentation: 'Declares which source property identifies an instance.',
        },
        {
            label: 'map',
            insertText: 'map\n    ${1:property} = ${2:source}',
            documentation: 'Maps and translates source values before events are appended.',
        },
        {
            label: 'append',
            insertText: 'append ${1:EventType}\n    when ${2:property}',
            documentation: 'Appends an event when the source data changes.',
        },
        {
            label: 'translate',
            insertText: 'translate\n    "${1:source}" => ${2:target}',
            documentation: 'Translates source values into Screenplay values.',
        },
        {
            label: 'children',
            insertText: 'children ${1:collection} identified by ${2:key}',
            documentation: 'Captures changes in a child collection of the source data.',
        },
        {
            label: 'when added',
            insertText: 'when added',
            documentation: 'Appends the event when a new item appears in the source.',
        },
        {
            label: 'when removed',
            insertText: 'when removed',
            documentation: 'Appends the event when an item disappears from the source.',
        },
    ],
    hovers: {
        source: 'CDL — declares where the captured data comes from.',
        api: 'CDL — the API used as the capture source.',
        route: 'CDL — the route on the source API to poll.',
        poll: 'CDL — the polling interval, e.g. 5m.',
        key: 'CDL — declares which source property identifies an instance.',
        map: 'CDL — maps and translates source values before events are appended.',
        translate: 'CDL — translates source values into Screenplay values.',
        append: 'CDL — appends an event when the source data changes.',
        when: 'CDL — the change condition: a property change, added, or removed.',
        added: 'CDL — matches items that appear in the source.',
        removed: 'CDL — matches items that disappear from the source.',
        children: 'CDL — captures changes in a child collection of the source data.',
        identified: 'CDL — introduces the identifying key of a child collection.',
    },
};
