// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';
import { Validator } from '../../sub-languages/projection/Validator';

export function modelFor(content: string): editor.ITextModel {
    return { getValue: () => content } as unknown as editor.ITextModel;
}

export function aProjectionValidator(): Validator {
    const validator = new Validator();
    validator.setReadModels([
        {
            identifier: 'Root', displayName: 'RootView', schema: {
                name: 'Root',
                properties: {
                    id: { type: 'string' },
                    rootOnly: { type: 'string' },
                    total: { type: 'number' },
                    scalar: { type: 'string' },
                    children: {
                        type: 'array', items: {
                            name: 'Child', properties: { childOnly: { type: 'string' }, id: { type: 'string' } },
                        },
                    },
                    loose: { items: { name: 'LooseChild', properties: { childOnly: { type: 'string' } } } },
                    emptyArray: { type: 'array' },
                },
                definitions: { Composite: { properties: {} } },
            },
        },
        { identifier: 'Variant', displayName: 'VariantView', schema: { name: 'Variant', properties: { variantOnly: { type: 'string' } } } },
    ]);
    validator.setEventSchemas({ Known: { name: 'Known', properties: {} } });
    return validator;
}
