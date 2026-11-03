// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { mergeDocuments } from '../Files/PlayFolderMerge';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ApplicationSyntax } from '../Syntax/Structure';
import { decodeExactSyntaxJson } from '../Syntax/StrictSyntaxJson';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const source = 'context\n  description "Calendar inputs"\n  bookingZone time zone\n  @reportingZone time zone';

describe('when parsing application context', () => {
    it('should retain typed members description and source locations', () => {
        const result = parse(source, 'context.play');
        result.diagnostics.should.deep.equal([]);
        const block = result.value.context!;
        block.kind.should.equal('ApplicationContextSyntax');
        block.description!.should.equal('Calendar inputs');
        block.location.should.include({ line: 1, path: 'context.play' });
        block.directiveLocations!.description.should.include({ line: 2, path: 'context.play' });
        block.members.map(member => [member.kind, member.name, member.location.line, member.location.path]).should.deep.equal([
            ['ContextMemberSyntax', 'bookingZone', 3, 'context.play'],
            ['ContextMemberSyntax', 'reportingZone', 4, 'context.play'],
        ]);
    });

    it('should round trip syntax and walk context children', () => {
        const result = parse('numbers exact\n' + source);
        const json = JSON.stringify(toSyntaxJson(result.value));
        const decoded = decodeExactSyntaxJson(json) as ApplicationSyntax;
        JSON.stringify(toSyntaxJson(decoded.context!)).should.equal(JSON.stringify(toSyntaxJson(result.value.context!)));
        class Walker extends ScreenplaySyntaxWalker {
            kinds: string[] = [];
            override visitNode(node: SyntaxNode): void { this.kinds.push(node.kind); }
        }
        const walker = new Walker();
        walker.visitApplication(result.value);
        walker.kinds.filter(kind => kind === 'ApplicationContextSyntax').should.have.lengthOf(1);
        walker.kinds.filter(kind => kind === 'ContextMemberSyntax').should.have.lengthOf(2);
    });

    it.each<[string, string, number]>([
        ['context named\n  zone time zone', 'PLAY0854', 1],
        ['context\ncontext', 'PLAY0854', 2],
        ['context\n  Zone time zone', 'PLAY0854', 2],
        ['context\n  zone time zone\n    nested String', 'PLAY0854', 2],
        ['context\n  zone String', 'PLAY0855', 2],
        ['context\n  zone time zone optional', 'PLAY0855', 2],
        ['context\n  zone time zone = "UTC"', 'PLAY0855', 2],
        ['context\n  zone time zone\n  @zone time zone', 'PLAY0856', 3],
        ['context\n  timeZone time zone', 'PLAY0857', 2],
        ['context\n  @identity time zone', 'PLAY0857', 2],
        ['context\n  description "one"\n  description "two"', 'PLAY0147', 3],
    ])('should diagnose %s', (text, code, line) => {
        parse(text).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal([`${code}@${line}`]);
    });

    it.each(['command', 'arguments', 'tenant', 'causedBy', 'causation', 'occurred', 'identity', 'timeZone'])('should reserve %s even when escaped', name => {
        parse(`context\n  @${name} time zone`).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0857']);
    });

    it('should reject context in placed documents', () => {
        const result = parse('context\n  zone time zone', 'module/context.play', ['M']);
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0854']);
        (result.value.context === undefined).should.be.true;
    });

    it('should keep the first folder context and reject an additional block', () => {
        const result = mergeDocuments([parse(source, 'first.play'), parse('context\n  zone time zone', 'second.play')]);
        result.value.context!.members.map(member => member.name).should.deep.equal(['bookingZone', 'reportingZone']);
        const duplicates = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0172');
        duplicates.should.have.lengthOf(1);
        duplicates[0].location.path!.should.equal('second.play');
    });

    it('should preserve valid members while recovering from invalid ones', () => {
        const result = parse('context\n  bad String\n  identity time zone\n  zone time zone\n  zone time zone\n  reportZone time zone');
        result.value.context!.members.map(member => member.name).should.deep.equal(['zone', 'reportZone']);
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0855', 'PLAY0857', 'PLAY0856']);
    });

    it('should not add a context member to legacy syntax JSON', () => {
        const result = parse('concept Context : String');
        (result.value.context === undefined).should.be.true;
        Object.keys(toSyntaxJson(result.value)!).should.not.contain('context');
    });

    it('should leave properties called context zone and timeZone contextual', () => {
        const result = parse('module M\n  feature F\n    slice StateChange S\n      command C\n        context String\n        zone String\n        timeZone String');
        result.diagnostics.should.deep.equal([]);
        result.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name).should.deep.equal(['context', 'zone', 'timeZone']);
    });
});
