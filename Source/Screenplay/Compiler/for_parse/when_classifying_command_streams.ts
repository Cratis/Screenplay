// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { compileApplication } from '../Files/PlayApplicationAssembly';

const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n        ';
const source = '\neventsource Account\n  identifier AccountId\n  stream Transactions\n    streamId Month\nconcept AccountId : Uuid\nconcept Month : String\n';
const command = (text: string) => parse(text).value.modules[0].features[0].slices[0].commands[0];

describe('when classifying command streams against the whole input', () => {
    it.each(['stream String', 'stream Account.Transactions optional', 'stream Account.Transactions[]', 'stream Account.Transactions generated identifier', '@stream Account.Transactions', 'identifier String', 'eventsource String', 'from String', 'streamId String'])('should retain property form %s', body => {
        const parsed = parse(prefix + body + source);
        expect(parsed.success).toBe(true);
        expect(command(prefix + body + source).stream).toBeNull();
        expect(command(prefix + body + source).properties).toHaveLength(1);
    });
    it.each(['stream Missing.Transactions\n          deeper String', '@stream Account.Transactions\n          deeper String'])('should retain deeper legacy members %s', body => {
        const parsed = parse(prefix + body + source);
        expect(parsed.success).toBe(true);
        expect(command(prefix + body + source).properties.map(property => property.name)).toEqual(['stream', 'deeper']);
        expect(command(prefix + body + source).stream).toBeNull();
    });
    it('should resolve a forward source and its nested mapping', () => {
        const parsed = parse(prefix + 'month Month\n        stream Account.Transactions\n          streamId = month' + source);
        expect(parsed.success).toBe(true);
        const node = parsed.value.modules[0].features[0].slices[0].commands[0];
        expect(node.stream?.streamId?.property).toBe('streamId');
        expect(node.properties.map(property => property.name)).toEqual(['month']);
    });
    it('should retain both viable interpretations', () => {
        const parsed = parse('import Account.Transactions\ntype Transactions\n  value String\n' + prefix + 'stream Account.Transactions' + source);
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0505');
        const node = parsed.value.modules[0].features[0].slices[0].commands[0];
        expect(node.properties[0].type.name).toBe('Account.Transactions');
        expect(node.stream?.propertyCandidate).not.toBeNull();
    });
    it('should retain ambiguity in every order of separately declared types sources and commands', () => {
        const documents: [string, string][] = [
            ['sources.play', 'eventsource Account\n  stream Transactions'],
            ['types.play', 'import Account.Transactions\ntype Transactions\n  value String'],
            ['commands.play', prefix + 'stream Account.Transactions\n          deeper String'],
        ];
        for (const first of documents) for (const second of documents.filter(document => document !== first)) {
            const third = documents.find(document => document !== first && document !== second)!;
            const result = compileApplication(new Map([first, second, third]));
            expect(result.success).toBe(false);
            expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0505');
            const node = result.value.modules[0].features[0].slices[0].commands[0];
            expect(node.stream?.propertyCandidate).not.toBeNull();
            expect(node.properties.map(property => property.name)).toEqual(['stream', 'deeper']);
        }
    });
    it.each([
        'domain Example\nimport Account.Transactions\n  type Transactions\n    value String\n',
        'import Account.Transactions\nimport "other.play"\n    type Transactions\n      value String\n',
        'import Account.Transactions\n  domain Example\n    type Transactions\n      value String\n',
        'import Account.Transactions\n\ttype Transactions\n\t  value String\n',
    ])('should follow real document leaf ownership for %s', declarations => {
        for (const text of [declarations + prefix + 'stream Account.Transactions\n          deeper String' + source, declarations + source + prefix + 'stream Account.Transactions\n          deeper String']) {
            const parsed = parse(text);
            expect(parsed.success).toBe(false);
            expect(parsed.value.types[0].name).toBe('Transactions');
            expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0505');
            const node = parsed.value.modules[0].features[0].slices[0].commands[0];
            expect(node.stream?.propertyCandidate?.type.name).toBe('Account.Transactions');
            expect(node.properties.map(property => property.name)).toEqual(['stream', 'deeper']);
            const escaped = command(text.replace('stream Account.Transactions', '@stream Account.Transactions'));
            expect(escaped.stream).toBeNull();
            expect(escaped.properties).toHaveLength(2);
        }
    });
    it('should not inventory keyword properties or fenced declarations', () => {
        const parsed = parse(prefix + 'eventsource String\n          type String\n        handler\n          ```csharp\ntype Transactions\neventsource Account\n  stream Transactions\n          ```\n');
        expect(parsed.value.types).toEqual([]);
        expect(parsed.value.eventSources).toEqual([]);
        expect(parsed.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name)).toEqual(['eventsource', 'type']);
    });
    it.each([
        'domain Example\nimport Account.Transactions\n  type Transactions\n    value String',
        'import Account.Transactions\n  type Transactions\n    value String',
        'import Account.Transactions\n\ttype Transactions\n\t  value String',
    ])('should inventory deeper declarations in every file order for %s', types => {
        const documents: [string, string][] = [
            ['sources.play', 'eventsource Account\n  stream Transactions'],
            ['types.play', types],
            ['commands.play', prefix + 'stream Account.Transactions\n          deeper String'],
        ];
        for (const first of documents) for (const second of documents.filter(document => document !== first)) {
            const third = documents.find(document => document !== first && document !== second)!;
            const result = compileApplication(new Map([first, second, third]));
            expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0505');
            const node = result.value.modules[0].features[0].slices[0].commands[0];
            expect(node.stream?.propertyCandidate).not.toBeNull();
            expect(node.properties).toHaveLength(2);
        }
    });
    it.each([false, true])('should use placed module and feature leaf rules in reverse order %s', reverse => {
        const documents: [string, string][] = [
            ['application.play', 'import Account.Transactions\neventsource Account\n  stream Transactions\nmodule M\n  import "module.play"\n  feature F\n    import "feature.play"'],
            ['module.play', 'description "Module"\n  type Transactions\n    value String'],
            ['feature.play', 'description "Feature"\n  slice StateChange S\n    command C\n      stream Account.Transactions\n        deeper String'],
        ];
        const result = compileApplication(new Map(reverse ? documents.reverse() : documents));
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0505');
        const node = result.value.modules[0].features[0].slices[0].commands[0];
        expect(node.stream?.propertyCandidate).not.toBeNull();
        expect(node.properties).toHaveLength(2);
    });
    it('should retain a real imported qualified type without a source', () => {
        const node = command('import Account.Transactions\ntype Transactions\n  value String\n' + prefix + 'stream Account.Transactions\n          deeper String');
        expect(node.stream).toBeNull();
        expect(node.properties).toHaveLength(2);
    });
});
