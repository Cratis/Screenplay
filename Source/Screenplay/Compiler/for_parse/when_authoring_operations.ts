// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { validateOperations } from '../Parsing/OperationValidator';
import { ParserContext } from '../Parsing/ParserContext';
import { LineReader } from '../Parsing/LineReader';

const encode = <Node extends SyntaxNode>(node: Node) => toSyntaxJson(node);
const prefix = 'system Mailer\nmodule M\n  feature F\n    slice StateChange S\n';
const declaration = '      operation Send\n        uses Mailer\n        recipient String\n        execute\n          implementation\n            hint "Use the adapter"\n        compensate\n          description "Undo"\n';
const command = '      command C\n        recipient String\n        produces Send\n          recipient = recipient\n';

describe('when authoring operations', () => {
    it.each([declaration + command, command + declaration, '      command C\n        recipient String\n        produces operation Send\n          uses Mailer\n          recipient String = recipient\n'])('should accept declaration-order-independent intent', body => {
        const result = parse(prefix + body);
        expect(result.diagnostics).toEqual([]);
        const slice = result.value.modules[0].features[0].slices[0];
        const resolution = new AuthoringProductionResolver(result.value).resolve('Send', slice);
        expect(resolution.kind).toBe('operation');
        expect(resolution.declaration?.scope).toEqual(['M', 'F', 'S']);
    });
    it.each([
        ['      operation Send\n        recipient String\n', 'PLAY0500'],
        ['      operation Send\n        uses Unknown\n', 'PLAY0500'],
        ['      operation Send\n        uses Mailer\n        uses Mailer\n', 'PLAY0500'],
        [declaration + '      event Send\n', 'PLAY0498'],
        [declaration + '      command C\n        produces Send\n', 'PLAY0501'],
        [declaration + '      command C\n        produces Send\n          recipient = 42\n', 'PLAY0501'],
        [declaration + '      specification T\n        when C\n        then compensated Send\n          ignored = 1\n', 'PLAY0502'],
        [declaration + '      reaction R\n        when Recorded\n          produces Send\n', 'PLAY0499'],
    ])('should reject invalid intent %s', (body, code) => expect(parse(prefix + body).diagnostics.map(diagnostic => diagnostic.code)).toContain(code));
    it('should not drain phase siblings', () => {
        const result = parse(prefix + declaration + '        extra String optional\n' + command);
        expect(result.success).toBe(true);
        expect(result.value.modules[0].features[0].slices[0].operations?.[0].inputs).toHaveLength(2);
    });
    it('should exclude operation productions before event destination aggregation', () => {
        const result = parse(prefix + declaration + '      command C\n        id Uuid identifier\n        recipient String\n        produces event Recorded\n          recipient String = recipient\n        produces Send\n          recipient = recipient\n');
        expect(result.diagnostics).toEqual([]);
    });
    it('should walk typed specification steps without event fabrication', () => {
        const result = parse(prefix + declaration + command + '      specification T\n        given operation Send fails\n        when C\n          recipient = "Ada"\n        then operation Send\n          recipient = "Ada"\n        then compensated Send\n');
        expect(result.success).toBe(true);
        class Walker extends ScreenplaySyntaxWalker { nodes: SyntaxNode[] = []; override visitNode(node: SyntaxNode): void { this.nodes.push(node); } }
        const walker = new Walker();
        walker.visitApplication(result.value);
        expect(walker.nodes.filter(node => node.kind === 'SpecificationOperationSyntax')).toHaveLength(1);
        expect(walker.nodes.filter(node => node.kind === 'OperationPhaseSyntax')).toHaveLength(2);
        expect(result.value.modules[0].features[0].slices[0].specifications[0].thenEvents).toEqual([]);
    });
    it('should resolve quoted imports and placed declarations independently of path order', () => {
        const result = compileApplication(new Map([
            ['root.play', 'system Mailer\nmodule M\n  feature F\n    import "operations.play"\n    import "command.play"'],
            ['operations.play', 'slice StateChange Other\n  operation Send\n    uses Mailer\n    recipient String'],
            ['command.play', 'slice StateChange Here\n  command C\n    recipient String\n    produces Other.Send\n      recipient = recipient'],
        ]), ['root.play']);
        expect(result.diagnostics).toEqual([]);
        const slices = result.value.modules[0].features[0].slices;
        expect(new AuthoringProductionResolver(result.value).resolve('Other.Send', slices.find(slice => slice.name === 'Here')!).kind).toBe('operation');
    });
    it.each(['system', 'operation', 'uses', 'execute', 'compensate'])('should preserve old command properties named %s with deeper indentation', name => {
        const result = parse(prefix + `      command C\n        ${name} String\n          deeper String\n`);
        expect(result.success).toBe(true);
        expect(result.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name)).toEqual([name, 'deeper']);
    });
    it('should not infer operation declarations from unknown productions', () => {
        const result = parse(prefix + '      command C\n        produces Typo\n');
        const slice = result.value.modules[0].features[0].slices[0];
        expect(slice.operations).toEqual([]);
        expect(new AuthoringProductionResolver(result.value).resolve('Typo', slice).kind).toBe('unresolved');
    });
    it.each([
        ['system Mailer extra\n', 'PLAY0495'],
        [prefix + '      operation Invalid extra\n', 'PLAY0496'],
        [prefix + '      operation Send\n        uses Mailer\n        name String identifier', 'PLAY0496'],
        [prefix + '      operation Send\n        uses Mailer\n        name String generated', 'PLAY0496'],
        [prefix + '      operation Send\n        uses Mailer\n        name String\n        name String', 'PLAY0168'],
        [prefix + '      operation Send\n        uses Mailer\n          nested', 'PLAY0500'],
        [prefix + '      operation Send\n        uses Mailer\n        name String\n          nested', 'PLAY0496'],
        [prefix + '      command C\n        produces operation Send\n          uses Mailer\n          name String', 'PLAY0501'],
        [prefix + '      operation Send\n        uses Mailer\n        execute\n        execute', 'PLAY0496'],
        [prefix + '      operation Send\n        uses Mailer\n        compensate\n        compensate', 'PLAY0496'],
        [prefix + '      operation Send\n        uses Mailer\n        execute\n          file One.cs\n          file Two.cs', 'PLAY0494'],
        [prefix + '      operation Send\n        uses Mailer\n        execute\n          implementation\n          file Two.cs', 'PLAY0494'],
        [prefix + '      operation Send\n        uses Mailer\n        execute\n          file One.cs\n            nested', 'PLAY0492'],
        [prefix + '      operation Send\n        uses Mailer\n        execute\n          unknown', 'PLAY0496'],
        [prefix + '      specification T\n        given operation Send', 'PLAY0502'],
        [prefix + '      specification T\n        then operation Send fails', 'PLAY0502'],
        [prefix + '      specification T\n        then compensated Send\n          nested', 'PLAY0502'],
        [prefix + declaration + '      command C\n        produces Send\n          unknown = 1\n          recipient = \"Ada\"\n          recipient = \"Ada\"', 'PLAY0501'],
        [prefix + declaration + '      command C\n        produces Send\n          recipient = missing', 'PLAY0501'],
        [prefix + declaration + '      command C\n        number Int\n        produces Send\n          recipient = number', 'PLAY0501'],
        [prefix + declaration + '      command C\n        produces Send\n          for missing\n          recipient = \"Ada\"', 'PLAY0496'],
        [prefix + declaration + '      command C\n        produces Send\n          tag audit\n          recipient = \"Ada\"', 'PLAY0496'],
        [prefix + '      operation Send\n        uses Mailer\n        value Missing', 'PLAY0165'],
        [prefix + '      operation Send\n        uses Mailer\n      command C\n        produces Send\n      specification T\n        when C\n        then compensated Send', 'PLAY0502'],
        [prefix + declaration + command + '      specification T\n        when C\n        then operation Unknown', 'PLAY0502'],
        [prefix + declaration + command + '      specification T\n        when C\n        given operation Send fails\n        given operation Send fails', 'PLAY0502'],
        [prefix + declaration + command + '      specification T\n        when C\n        then operation Send\n          recipient = recipient', 'PLAY0502'],
        [prefix + declaration + '      command C\n      specification T\n        when C\n        then operation Send', 'PLAY0502'],
        [prefix + declaration + '      specification T\n        when query Q\n        then operation Send', 'PLAY0502'],
    ])('should diagnose malformed and statically incompatible authoring %s', (source, code) => {
        expect(parse(source).diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
    });
    it.each(['{}', '{"recipient":"Ada"}'])('should require complete composite production inputs for %s', value => {
        const result = parse('type Contact\n  recipient String\n' + prefix + '      operation Send\n        uses Mailer\n        contact Contact\n      command C\n        produces Send\n          contact = ' + value);
        expect(result.success).toBe(value !== '{}');
    });
    it('should retain collection, optional, nominal and quoted enum shape checks without guessing imports', () => {
        const declarations = 'concept Channel : Enum\n  email\nconcept Name : String\ntype Contact\n  name Name\nimport External.Payload\n';
        const operation = '      operation Send\n        uses Mailer\n        channels Channel[]\n        contacts Contact[]\n        extra Payload\n        note String optional\n';
        const input = '      command C\n        contacts Contact[]\n        produces Send\n          channels = ["email"]\n          contacts = contacts\n          extra.unknown = "not inferred"\n          extra = "unavailable shape"\n          note = null\n';
        expect(parse(declarations + prefix + operation + input).diagnostics).toEqual([]);
        expect(parse(declarations + prefix + operation + input.replace('["email"]', '["unknown"]')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0501');
    });
    it('should accept complete nested composite paths and preserve optional parent types', () => {
        const types = 'type Contact\n  name String\n  note String optional\n';
        const source = types + prefix + '      operation Send\n        uses Mailer\n        contact Contact\n        note String optional\n      command C\n        source Contact optional\n        produces Send\n          contact.name = "Ada"\n          note = source.note\n';
        expect(parse(source).diagnostics).toEqual([]);
    });
    it('should keep declared read-backed mapping sources separate from command fields', () => {
        const source = prefix + declaration + '      readmodel View\n        recipient String\n      command C\n        reads View as view\n        produces Send\n          recipient = view.recipient\n';
        expect(parse(source).diagnostics).toEqual([]);
        expect(parse(source.replace('view.recipient', 'view.missing')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0501');
        expect(parse(source.replace('      readmodel View\n        recipient String\n', '')) .diagnostics).toEqual([]);
    });
    it('should attach exactly one wrapped or direct source to the owning phase', () => {
        const source = prefix + '      operation Send\n        uses Mailer\n        execute\n          description "Execute"\n          implementation\n            hint "One"\n            ```csharp\n            // unchanged\n            ```\n        compensate\n          file Undo.cs\n';
        const result = parse(source);
        expect(result.diagnostics).toEqual([]);
        const operation = result.value.modules[0].features[0].slices[0].operations![0];
        expect(operation.execute?.code?.code).toBe('// unchanged');
        expect(operation.compensate?.file?.path).toBe('Undo.cs');
        expect(() => encode({ ...operation.execute!, file: operation.compensate!.file })).toThrow();
        expect(() => encode({ ...operation.execute!, implementation: { ...operation.execute!.implementation!, hints: [{ kind: 'ImplementationHintSyntax', text: ' ', location: operation.location }] } })).toThrow();
    });
    it('should reject incompatible double-inline and misaligned programmatic productions', () => {
        const slice = parse(prefix + '      command C\n        produces operation Send\n          uses Mailer\n').value.modules[0].features[0].slices[0];
        const production = slice.commands[0].produces[0];
        expect(() => encode({ ...production, event: 'Other' })).toThrow();
        expect(() => encode({ ...production, inlineEvent: { kind: 'EventSyntax', name: 'Send', properties: [], tags: [], id: null, description: null, documentation: null, generation: 1, hasGenerationMarker: false, location: production.location } })).toThrow();
        expect(() => encode({ ...production, mappings: [{ kind: 'PropertyMappingSyntax', property: 'extra', source: { kind: 'LiteralExpressionSyntax', value: 1, location: production.location }, location: production.location }] })).toThrow();
    });
    it.each([
        ['system Mailer extra\n  description "Invalid header"', 'PLAY0495'],
        [prefix + '      operation Send\n        uses Mailer\n        for wrong', 'PLAY0496'],
        [prefix + '      operation Send\n        uses Mailer\n        execute\n          file First.cs\n          implementation\n            hint "Conflicts"', 'PLAY0494'],
        [prefix + '      operation Send\n        uses Mailer\n        execute\n          implementation\n          implementation', 'PLAY0494'],
        [prefix + '      operation Send\n        uses Mailer\n        execute\n          ```csharp\n          direct();\n          ```\n          file Other.cs', 'PLAY0494'],
        [prefix + '      event Send\n      operation Send\n        uses Mailer\n      command C\n        produces Send', 'PLAY0497'],
        [prefix + '      event Recorded\n      reaction R\n        every 1 day\n          produces S.Recorded', 'PLAY0497'],
        [prefix + declaration + '      specification T\n        then operation Send', 'PLAY0502'],
        ['system Mailer\nsystem Mailer\n', 'PLAY0495'],
    ])('should keep ambiguous and malformed authoring failures explicit %s', (source, code) => {
        expect(parse(source).diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
    });
    it('should not select an operation from an opaque contract import or an unknown owning scope', () => {
        const result = parse('import External.Send\n' + prefix + '      command C\n        produces Send\n');
        const slice = result.value.modules[0].features[0].slices[0];
        const resolver = new AuthoringProductionResolver(result.value);
        expect(resolver.resolve('Send', slice).kind).toBe('unresolved');
        expect(resolver.resolve('Send', { ...slice }).kind).toBe('unresolved');
    });
    it('should collapse generations into one current event candidate', () => {
        const syntax = parse(prefix + '      event Recorded generation 2\n      event Recorded generation 1\n').value;
        const resolver = new AuthoringProductionResolver(syntax);
        const result = resolver.resolve('Recorded', syntax.modules[0].features[0].slices[0]);
        expect(result.kind).toBe('event');
        expect(result.declaration?.node.kind === 'EventSyntax' && result.declaration.node.generation).toBe(2);
        expect(resolver.declarations).toHaveLength(1);
    });
    it('should preserve a source collection or optional composite through nested paths', () => {
        const types = 'type Contact\n  name String\n';
        const source = types + prefix + '      operation Send\n        uses Mailer\n        names String[]\n      command C\n        contacts Contact[]\n        produces Send\n          names = contacts.name\n';
        expect(parse(source).diagnostics).toEqual([]);
        expect(parse(source.replace('names String[]', 'names String')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0501');
    });
    it('should validate programmatic qualified command association and retain old missing member defaults', () => {
        const syntax = parse(prefix + declaration + command + '      specification T\n        when C\n        then operation Send\n').value;
        const module = syntax.modules[0];
        const feature = module.features[0];
        const slice = feature.slices[0];
        const specification = slice.specifications[0];
        const application = { ...syntax, modules: [{ ...module, features: [{ ...feature, slices: [{ ...slice, specifications: [{ ...specification, when: { ...specification.when!, commandType: 'S.C' } }] }] }] }] };
        const context = new ParserContext(new LineReader([]));
        validateOperations(application, context);
        expect(context.diagnostics).toEqual([]);
        const old = { ...syntax, systems: undefined };
        const unavailable = new ParserContext(new LineReader([]));
        validateOperations(old, unavailable);
        expect(unavailable.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0500');
    });
    it('should treat duplicate or unavailable mapping source shapes as unknown, not guessed', () => {
        const source = prefix + declaration + '      command C\n        recipient String\n        recipient Int\n        produces Send\n          recipient = recipient\n';
        expect(parse(source).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0501')).toEqual([]);
        const read = prefix + declaration + '      command C\n        reads External as view\n        produces Send\n          recipient = view\n';
        expect(parse(read).diagnostics).toEqual([]);
    });
    it('should validate required collection literals and recursive composites without claiming completeness', () => {
        const source = 'type Contact\n  name String\n' + prefix + '      operation Send\n        uses Mailer\n        contacts Contact[]\n      command C\n        produces Send\n          contacts = [{"name":"Ada"}]\n';
        expect(parse(source).diagnostics).toEqual([]);
        expect(parse(source.replace('[{"name":"Ada"}]', '[{}]')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0501');
        const recursive = 'type Contact\n  next Contact\n' + prefix + '      operation Send\n        uses Mailer\n        contact Contact\n      command C\n        produces Send\n          contact.next = {}';
        expect(parse(recursive).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0501');
    });
    it('should reject operation ambiguity before computing event defaults or reaction consequences', () => {
        const source = 'system Mailer\nmodule M\n  feature F\n    slice StateChange A\n      operation Send\n        uses Mailer\n    slice StateChange B\n      operation Send\n        uses Mailer\n    slice StateChange Here\n      command C\n        id Uuid identifier\n        produces event Recorded\n        produces Send\n      reaction R\n        every 1 day\n          produces Send\n';
        const result = parse(source);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0497')).toHaveLength(2);
        expect(result.diagnostics.filter(diagnostic => ['PLAY0470', 'PLAY0478', 'PLAY0166'].includes(diagnostic.code))).toEqual([]);
    });
    it('should reject qualified event productions without broadening event grammar', () => {
        const result = parse(prefix + '      event Recorded\n      command C\n        produces S.Recorded\n');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0497');
    });
});
