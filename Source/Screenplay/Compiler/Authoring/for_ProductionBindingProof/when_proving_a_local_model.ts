// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { splitLines } from '../../Parsing/SourceLineSplitter';
import { parse } from '../../ScreenplayCompiler';
import { ProductionBindingProof } from '../ProductionBindingProof';

const source = 'module M\n  feature F\n    slice StateChange S\n      command C\n        projectId Uuid identifier\n        produces E\n          projectId = projectId\n      event E\n        projectId Uuid';
const permits = (text: string) => new ProductionBindingProof(parse(text).value).permits(splitLines(text));

describe('when proving a local command and event model', () => {
    it('should admit a complete property mapping', () => expect(permits(source)).toBe(true));

    it('should permit an unmapped optional property', () => {
        expect(permits(source + '\n        note String optional')).toBe(true);
    });

    it.each([
        source.replace('StateChange', 'StateView'),
        source + '\n      event E',
        source.replace('        projectId Uuid identifier', '        projectId Missing identifier'),
        source.replace('projectId = projectId', 'projectId = "literal"'),
        source.replace('projectId = projectId', 'missing = projectId'),
        source.replace('projectId = projectId', 'projectId = projectId\n          projectId = projectId'),
        source.replace('projectId = projectId', 'projectId = $context.identity.id'),
        source.replace('projectId = projectId', 'projectId = $context.occurred'),
        source.replace('        produces E', '        produces E\n          for "literal"'),
        source.replace('        produces E', '        produces E\n          for missing'),
        source.replace('        produces E', '        produces E\n          for projectId').replace('Uuid identifier', 'Uuid optional identifier'),
        source.replace('        produces E', '        produces E\n          for projectId').replace('Uuid identifier', 'Uuid[] identifier'),
        source.replace('        produces E', '        produces E\n          for projectId').replace(' identifier', ''),
        source.replace('      event E', '      event E\n        id "E"'),
        source.replace('      event E', '      event E generation 1'),
        source.replace('        projectId Uuid identifier', '        projectId Uuid identifier\n        projectId Uuid'),
        source + '\n  feature F',
        source + '\n    slice StateChange S',
        source + '\n      command C',
        source + '\n    feature Child\n    feature Child',
        'concept Uuid : String\n' + source,
        'concept Note : String @pii\n' + source,
        'concept Choice : Enum\n  one\n' + source,
        source.replace('      command C', '      command C\n        handler\n          ```csharp\n          return null;\n          ```'),
    ])('should refuse an unproven document: %s', text => expect(permits(text)).toBe(false));

    it('should refuse duplicate identifier flags even on a directly supplied tree', () => {
        const parsed = parse(source).value;
        const command = parsed.modules[0].features[0].slices[0].commands[0];
        Object.assign(command, { properties: [...command.properties, { ...command.properties[0], name: 'otherId' }] });
        expect(new ProductionBindingProof(parsed).permits(splitLines(source))).toBe(false);
    });
});
