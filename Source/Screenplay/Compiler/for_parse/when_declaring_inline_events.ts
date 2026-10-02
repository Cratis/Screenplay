// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { implicitDestination } from '../Syntax/ProductionDestinations';

const prefix = 'module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        otherId Uuid\n        name String\n';
const inline = `${prefix}        produces event Renamed\n`;

describe('when declaring inline events', () => {
    it('should retain the declaration, typed mappings and event tags', () => {
        const result = parse(`${inline}          description "A new name"\n          documentation\n            \`\`\`markdown\n            More **details**.\n            \`\`\`\n          tag audit\n          name String = name\n`);
        expect(result.diagnostics).toEqual([]);
        const slice = result.value.modules[0].features[0].slices[0];
        expect(slice.events).toEqual([]);
        const event = eventDeclarations(slice)[0];
        expect(event.description).toBe('A new name');
        expect(event.documentation).toBe('More **details**.');
        expect(event.tags).toHaveLength(1);
        expect(slice.commands[0].produces[0].tags).toEqual([]);
        expect(implicitDestination(slice.commands[0], slice.commands[0].produces[0])).toBe('projectId');
    });

    it.each(['namespace', 'sequence', 'correlation', 'causation', 'causedBy', 'occurred', 'origin'])('should reject reserved %s', keyword => {
        expect(parse(`${inline}          ${keyword} supplied\n`).diagnostics.map(value => value.code)).toContain('PLAY0476');
    });

    it.each([
        ['          generation 2\n', 'PLAY0475'],
        ['          id ""\n', 'PLAY0472'],
        ['          id "Old"\n          id "Other"\n', 'PLAY0472'],
        ['          documentation "not fenced"\n', 'PLAY0477'],
        ['          name = name\n', 'PLAY0044'],
        ['          for projectId\n          for projectId\n', 'PLAY0193'],
        ['          projectId Uuid = projectId\n', 'PLAY0469'],
        ['          id "Renamed"\n', 'PLAY0471'],
        ['        produces event Other\n          for otherId\n', 'PLAY0470'],
        ['      event Renamed\n', 'PLAY0473'],
    ])('should diagnose %s', (body, code) => {
        expect(parse(inline + body).diagnostics.map(value => value.code)).toContain(code);
    });

    it('should reject reaction declarations', () => {
        expect(parse('module Projects\n  feature Naming\n    slice Automation React\n      reaction React\n        every 1 day\n          produces event Renamed\n').diagnostics.map(value => value.code)).toContain('PLAY0474');
    });

    it('should preserve escaped payload names and property-shaped metadata names', () => {
        const result = parse(`${inline}          @sequence String = name\n          id String = name\n          description String = name\n`);
        expect(result.diagnostics).toEqual([]);
        expect(eventDeclarations(result.value.modules[0].features[0].slices[0])[0].properties.map(value => value.name)).toEqual(['sequence', 'id', 'description']);
    });

    it('should suppress destination hints for mixed sources', () => {
        const command = parse(`${inline}        produces event Other\n          for otherId\n`).value.modules[0].features[0].slices[0].commands[0];
        expect(implicitDestination(command, command.produces[0])).toBeUndefined();
    });

    it('should distinguish legacy allocation from inline defaults', () => {
        const command = parse(`${prefix}        produces Renamed\n`).value.modules[0].features[0].slices[0].commands[0];
        expect(implicitDestination(command, command.produces[0])).toBe('new event source');
    });
});
