// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { compileApplication, parsePlacedDocuments } from '../Files/PlayApplicationAssembly';
import { EventSourceCatalog } from '../Syntax/EventSourceCatalog';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { validateEventSources } from '../Parsing/EventSourceValidator';
import { ParserContext } from '../Parsing/ParserContext';
import { LineReader } from '../Parsing/LineReader';

const prefix = 'concept AccountId : Uuid\nconcept Month : String\neventsource Account\n  identifier AccountId\n  stream Transactions\n    streamId Month\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id AccountId identifier\n        month Month\n';
const route = '        stream Account.Transactions\n          streamId = month\n';
const command = (text: string) => parse(text).value.modules[0].features[0].slices[0].commands[0];

describe('when authoring source-owned streams', () => {
    it.each([
        ['eventsource A\n  stream S\n  stream S', 'PLAY0503'],
        ['eventsource A\neventsource A', 'PLAY0503'],
        ['eventsource A\n  identifier String optional', 'PLAY0503'],
        ['eventsource A\n  identifier String[]', 'PLAY0503'],
        ['type Composite\n  value String\neventsource A\n  identifier Composite', 'PLAY0503'],
        ['eventsource A\n  stream S\n    streamId Int', 'PLAY0506'],
        ['eventsource A\n  stream S\n    streamId Decimal', 'PLAY0506'],
        ['eventsource A\n  stream S\n    streamId Bool', 'PLAY0506'],
        ['eventsource A\n  stream S\n    streamId DateTime', 'PLAY0506'],
        ['concept Choice : Enum\n  one\neventsource A\n  stream S\n    streamId Choice', 'PLAY0506'],
        [prefix + '        stream Account.Missing', 'PLAY0504'],
        [prefix + '        stream Account.Transactions', 'PLAY0504'],
        [prefix + '        stream Account.Transactions\n          streamId = missing', 'PLAY0504'],
        [prefix + '        stream Account.Transactions\n          streamId = id', 'PLAY0504'],
        [prefix + '        stream Account.Transactions\n          streamId = 42', 'PLAY0504'],
        [prefix + route + route, 'PLAY0504'],
        ['eventsource A invalid', 'PLAY0503'],
        ['eventsource A\n  stream S invalid', 'PLAY0503'],
        ['eventsource A\n  unknown\n    deeper', 'PLAY0503'],
        ['eventsource A\n  stream S\n    unknown\n      deeper', 'PLAY0503'],
        ['eventsource A\n  identifier', 'PLAY0503'],
        ['eventsource A\n  identifier Uuid\n  identifier String', 'PLAY0503'],
        ['eventsource A\n  identifier Uuid\n    deeper', 'PLAY0503'],
        ['eventsource A\n  id ""', 'PLAY0503'],
        ['eventsource A\n  id invalid', 'PLAY0503'],
        ['eventsource A\n  id "old"\n  id "older"', 'PLAY0503'],
        ['eventsource A\n  id "old"\n    deeper', 'PLAY0503'],
        ['eventsource A\n  id "A"', 'PLAY0507'],
        [prefix + '        stream Account.Transactions\n          invalid\n            nested', 'PLAY0504'],
        [prefix + route + '          streamId = month', 'PLAY0504'],
        [prefix + route + '            deeper', 'PLAY0504'],
        [prefix + '        stream Account.Transactions\n          streamId = month + 1', 'PLAY0504'],
        [prefix + '        stream Account.Transactions\n          streamId = {"value": 1}', 'PLAY0504'],
        [prefix + '        stream Account.Transactions\n          streamId = [1]', 'PLAY0504'],
        [prefix + '        stream Account.Transactions\n          streamId = null', 'PLAY0504'],
        [prefix + '        stream Account.Transactions\n          streamId = month.child', 'PLAY0504'],
        ['eventsource A\n  stream S\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream A.S\n          streamId = "extra"', 'PLAY0504'],
        ['eventsource A\n  stream S\n    streamId Missing', 'PLAY0165'],
    ])('should diagnose invalid authoring %s', (source, code) => expect(parse(source).diagnostics.map(diagnostic => diagnostic.code)).toContain(code));

    it.each(['String', 'Uuid', 'Month', 'Label', 'Key'])('should accept portable stream type %s without formatting', type => expect(parse('concept Month : Int\nconcept Label : String\nconcept Key : Uuid\neventsource A\n  stream S\n    streamId ' + type).success).toBe(true));
    it('should preserve integer concept mapping rules without admitting bare integer declarations', () => {
        const source = prefix.replace('concept Month : String', 'concept Month : Int');
        expect(parse(source + route).success).toBe(true);
        expect(parse(source + route.replace('streamId = month', 'streamId = 42')).success).toBe(true);
        expect(parse(source.replace('month Month', 'month Int') + route).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0504');
    });
    it('should keep unavailable imported shapes unresolved', () => expect(parse('import Contracts.Unknown\neventsource A\n  identifier Unknown\n  stream S\n    streamId Unknown').diagnostics).toEqual([]));
    it('should refuse a source identifier mismatch as an authoring error', () => {
        const parsed = parse(prefix.replace('id AccountId identifier', 'id Uuid identifier') + route);
        expect(parsed.success).toBe(false);
        expect(parsed.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0504' && diagnostic.severity === 'error')).toBe(true);
    });
    it('should allow handler authoring without statically produced events and keep the old handler ban', () => {
        expect(parse(prefix + route + '        handler\n          file C.cs').success).toBe(true);
        expect(parse(prefix + route + '        handler\n          file C.cs\n        produces event Recorded').diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0035');
    });
    it('should not capture production payload keywords', () => {
        const node = command(prefix + '        produces event Recorded\n          stream String = month\n          streamId String = month\n          eventsource String = month\n          identifier String = month\n          from String = month');
        expect(node.stream).toBeNull();
        expect(node.produces[0].mappings.map(mapping => mapping.property)).toEqual(['stream', 'streamId', 'eventsource', 'identifier', 'from']);
    });
    it('should keep plain untyped mapping payloads unchanged', () => {
        const node = command(prefix + '        produces Recorded\n          stream = month\n          streamId = month');
        expect(node.stream).toBeNull();
        expect(node.produces[0].for).toBeNull();
        expect(node.produces[0].mappings.map(mapping => mapping.property)).toEqual(['stream', 'streamId']);
    });
    it('should capture fields, pins, fences, CRLF, metadata spans and walker children', () => {
        const text = 'eventsource Account // source\n  description "Account"\n  id "OldAccount"\n  identifier AccountId\n  stream Transactions\n    description\n      ```markdown\n      History\n      ```\n    id "OldTransactions"\n    streamId Month\nconcept AccountId : Uuid\nconcept Month : String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        month Month\n        stream Account.Transactions // route\n          streamId = month // mapping\n';
        const parsed = parse(text.replaceAll('\n', '\r\n'), 'input.play');
        expect(parsed.success).toBe(true);
        expect(parsed.value.eventSources?.[0].streams[0].description).toBe('History');
        const node = parsed.value.modules[0].features[0].slices[0].commands[0];
        expect(node.stream?.referenceLocation).toEqual({ line: 19, column: 16, path: 'input.play' });
        expect(node.stream?.referenceLength).toBe('Account.Transactions'.length);
        expect(node.stream?.streamId?.source.location).toEqual({ line: 20, column: 22, path: 'input.play' });
        expect(JSON.stringify(toSyntaxJson(parsed.value))).not.toContain('referenceLocation');
        class Walker extends ScreenplaySyntaxWalker { nodes: SyntaxNode[] = []; override visitNode(node: SyntaxNode): void { this.nodes.push(node); } }
        const walker = new Walker(); walker.visitApplication(parsed.value);
        for (const kind of ['EventSourceSyntax', 'EventStreamSyntax', 'CommandStreamSyntax', 'PropertyMappingSyntax']) expect(walker.nodes.some(node => node.kind === kind)).toBe(true);
        const ambiguous = parse('import Account.Transactions\ntype Transactions\n  value String\n' + prefix + '        stream Account.Transactions');
        walker.visitApplication(ambiguous.value);
        expect(ambiguous.value.modules[0].features[0].slices[0].commands[0].streamCandidates?.[0].propertyCandidate).not.toBeNull();
    });
    it.each([false, true])('should resolve native imports and placed barrels independently of file order %s', reverse => {
        const entries: [string, string][] = [
            ['root.play', 'import "sources.play"\nmodule M\n  feature F\n    import "barrel.play"'],
            ['sources.play', 'concept AccountId : Uuid\nconcept Month : String\neventsource Account\n  identifier AccountId\n  stream Transactions\n    streamId Month'],
            ['barrel.play', 'import "command.play"'],
            ['command.play', 'slice StateChange S\n  command C\n    month Month\n    stream Account.Transactions\n      streamId = month'],
        ];
        const result = compileApplication(new Map(reverse ? entries.reverse() : entries));
        expect(result.success).toBe(true);
        expect(result.value.modules[0].features[0].slices[0].commands[0].stream?.eventSource).toBe('Account');
        const duplicate = compileApplication(new Map([...entries, ['duplicate.play', 'eventsource Account\n  stream Other']]));
        expect(duplicate.value.eventSources).toHaveLength(2);
        expect(new EventSourceCatalog(duplicate.value).resolve('Account', 'Transactions').kind).toBe('ambiguous');
    });
    it('should exclude descendants of conflicting barrels from authoritative source candidates', () => {
        const result = compileApplication(new Map([
            ['root.play', 'module One\n  import "barrel.play"\nmodule Two\n  import "barrel.play"'],
            ['barrel.play', 'import "sources.play"'],
            ['sources.play', 'eventsource Account\n  stream Transactions'],
        ]), ['root.play']);
        expect(result.success).toBe(false);
        expect(result.value.eventSources).toEqual([]);
        expect(result.documents.filter(document => document.path !== 'root.play').every(document => document.isPlacementResolved === false)).toBe(true);
    });
    it('should make exact source scope and wrong-kind states observable without suffix lookup', () => {
        const application = parse('type Value\n  value String\neventsource A\n  stream Same\neventsource B\n  stream Same\neventsource DuplicateStream\n  stream Same\n  stream Same').value;
        const catalog = new EventSourceCatalog(application);
        expect(catalog.resolve('A', 'Same').kind).toBe('unique');
        expect(catalog.resolve('B', 'Same').kind).toBe('unique');
        expect(catalog.resolve('Value', 'Same').kind).toBe('wrongKind');
        expect(catalog.resolve('Unknown', 'Same').kind).toBe('notFound');
        expect(catalog.resolve('A', 'Missing').kind).toBe('notFound');
        expect(catalog.resolve('DuplicateStream', 'Same').kind).toBe('ambiguous');
        expect(catalog.resolve('Module.A', 'Same').kind).toBe('notFound');
    });
    it('should preserve unresolved nested imported mapping shapes and detect known missing fields', () => {
        const text = 'type Input\n  month Month\nconcept Month : String\neventsource A\n  stream S\n    streamId Month\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        input Input\n        stream A.S\n          streamId = input.month';
        expect(parse(text).success).toBe(true);
        expect(parse(text.replace('input.month', 'input.missing')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0504');
        expect(parse(text.replace('type Input\n  month Month', 'import Shapes.Input')).success).toBe(true);
        expect(parse(text.replace('type Input\n  month Month', 'type Input\n  month Month\n  month Month')).diagnostics.some(diagnostic => diagnostic.code === 'PLAY0504')).toBe(false);
    });
    it('should validate programmatic unresolved/wrong-kind routes without claiming validity', () => {
        const parsed = parse(prefix + route).value;
        const module = parsed.modules[0]; const feature = module.features[0]; const slice = feature.slices[0]; const command = slice.commands[0];
        for (const source of ['Month', 'Missing']) {
            const application = { ...parsed, modules: [{ ...module, features: [{ ...feature, slices: [{ ...slice, commands: [{ ...command, stream: { ...command.stream!, eventSource: source } }] }] }] }] };
            const context = new ParserContext(new LineReader([])); validateEventSources(application, context);
            expect(context.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0504');
        }
        expect(parsePlacedDocuments([{ path: 'draft.play', source: 'eventsource A', placement: [], isPlacementResolved: false }]).value.eventSources).toEqual([]);
    });
    it('should match Unicode word characters in source and stream names', () => {
        const parsed = parse((prefix + route).replaceAll('Account', 'Accountß').replaceAll('Transactions', 'Transαctions'));
        expect(parsed.success).toBe(true);
        expect(parsed.value.modules[0].features[0].slices[0].commands[0].stream?.stream).toBe('Transαctions');
        expect(() => toSyntaxJson(parsed.value)).not.toThrow();
    });
    it.each(['Input optional', 'Input[]'])('should retain composite mapping parent flags %s', type => {
        const text = 'type Input\n  month Month\nconcept Month : String\neventsource A\n  stream S\n    streamId Month\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        input ' + type + '\n        stream A.S\n          streamId = input.month';
        expect(parse(text).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0504');
    });
    it.each(['eventsource Account\n  stream Other', 'eventsource Account\n  stream Transactions\n  stream Transactions', 'eventsource Account\n  stream Transactions\neventsource Account\n  stream Other'])('should report unresolved route ownership before property ambiguity %s', declarations => {
        const parsed = parse('import Account.Transactions\ntype Transactions\n  value String\n' + declarations + '\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Transactions');
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0504');
        expect(parsed.diagnostics.map(diagnostic => diagnostic.code)).not.toContain('PLAY0505');
    });
    it('should not discover source headers inside unindented fenced descriptions', () => {
        const parsed = parse('eventsource Real\n  description\n    ```text\neventsource Fake\n    ```\n  stream S\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Fake.S\n          deeper String');
        expect(parsed.value.eventSources).toHaveLength(1);
        expect(parsed.value.modules[0].features[0].slices[0].commands[0].stream).toBeNull();
        expect(parsed.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name)).toEqual(['stream', 'deeper']);
    });
});
