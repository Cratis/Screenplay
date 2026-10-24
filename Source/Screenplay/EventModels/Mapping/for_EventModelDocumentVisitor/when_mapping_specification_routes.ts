// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { readEventModelDocument, SpecificationHeader, SpecificationRunState } from '@cratis/event-models';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';

const source = readFileSync(new URL('../../../Compiler/Conformance/specification-streams.play', import.meta.url), 'utf8').replace('numbers exact\n', '');

describe('when mapping specification routes', () => {
    it('should show authored routing separately from payload values on every event role', () => {
        const document = toEventModelDocument(parse(source).value, 'Banking');
        const specification = document.collections[0].modules[0].features[0].slices[0].specifications[0];
        specification.given[0].name.should.contain('stream Account.Transactions');
        specification.given[0].name.should.contain('streamId = "p-1:2026-10"');
        specification.given[0].name.should.contain('for "other"');
        specification.given[0].values.should.deep.equal({ stream: 5 });
        specification.whenEvent!.name.should.contain('stream Account.Profile');
        specification.whenEvent!.values.should.deep.equal({ streamId: 6 });
        specification.thenEvents[0].name.should.contain('stream Account.Partitioned');
        specification.thenEvents[0].name.should.contain('streamId = 202610');
        specification.thenEvents[1].name.should.contain('no stream');
        specification.thenEvents[2].name.should.equal('Recorded');
        specification.given[0].name.should.contain('Admitted by ESM v8');
        specification.given[0].name.should.not.contain('PLAY0268');
    });
    it('should state the routing of the whole specification as information annotations rather than in its title', () => {
        const slice = toEventModelDocument(parse(source).value, 'Banking').collections[0].modules[0].features[0].slices[0];
        const specification = slice.specifications[0];
        specification.name.should.equal('ReadingHistory');
        slice.annotations!.every(annotation => annotation.severity === 'information').should.be.true;
        const messages = slice.annotations!.filter(annotation => annotation.specificationId === specification.id).map(annotation => annotation.message);
        messages[0].should.equal('given 1: Recorded — for "other"; stream Account.Transactions; streamId = "p-1:2026-10"');
        messages.should.include('then 2: Recorded — no stream');
        messages.filter(message => message.startsWith('when append: Recorded')).should.have.length(1);
        messages[messages.length - 1].should.contain('Admitted by ESM v8');
        messages.join('\n').should.not.contain('PLAY0268');
        problems_the_board_finds_in(toEventModelDocument(parse(source).value, 'Banking')).should.deep.equal([]);
    });
    it('should target each routed specification by its own id', () => {
        const slice = toEventModelDocument(parse(source).value, 'Banking').collections[0].modules[0].features[0].slices[0];
        slice.specifications.should.have.length(2);
        [...new Set(slice.annotations!.map(annotation => annotation.specificationId))].should.deep.equal(slice.specifications.map(specification => specification.id));
    });
    it('should render every route in the header badge even when given and then events are owned', () => {
        const document = toEventModelDocument(parse(source.replace('slice StateView History', 'slice StateChange History')).value, 'Banking');
        problems_the_board_finds_in(document).should.deep.equal([]);
        const slice = document.collections[0].modules[0].features[0].slices[0];
        slice.specifications[0].given[0].eventId.should.equal(slice.events[0].id);
        slice.specifications[0].thenEvents[0].eventId.should.equal(slice.events[0].id);
        const board = readEventModelDocument(document);
        const specification = board.collections[0].modules[0].features[0].slices[0].specifications[0];
        const annotations = board.collections[0].modules[0].features[0].slices[0].annotations!.filter(annotation => annotation.specificationId?.toString() === specification.id.toString());
        const markup = renderToStaticMarkup(createElement(SpecificationHeader, {
            name: specification.name, collapsed: specification.collapsed, runState: SpecificationRunState.Idle, annotations,
        }));
        markup.should.contain('given 1: Recorded');
        markup.should.contain('stream Account.Transactions; streamId = &quot;p-1:2026-10&quot;');
        markup.should.contain('when append: Recorded');
        markup.should.contain('stream Account.Profile');
        markup.should.contain('then 1: Recorded');
        markup.should.contain('stream Account.Partitioned; streamId = 202610');
        markup.should.contain('then 2: Recorded — no stream');
        markup.should.contain('Admitted by ESM v8');
        markup.should.not.contain('PLAY0268');
    });
    it.each([
        ['ReadingHistory', 'Reading History'],
        ['readingHistory', 'reading History'],
        ['UEDecision2ForAccount3', 'UE Decision 2 For Account 3'],
        ['Reading history', 'Reading history'],
    ])('should leave the routed base title %s to the board to split', (name) => {
        const application = parse(source).value;
        const document = toEventModelDocument({ ...application, modules: application.modules.map(module => ({
            ...module, features: module.features.map(feature => ({
                ...feature, slices: feature.slices.map(slice => ({
                    ...slice, specifications: slice.specifications.map(specification => ({ ...specification, name })),
                })),
            })),
        })) }, 'Banking');
        document.collections[0].modules[0].features[0].slices[0].specifications[0].name.should.equal(name);
    });
    it.each(['McpApp/board.css', 'VSCodeExtension/Webview/board.css'])('should leave the specification header styling to the board in %s', path => {
        const css = readFileSync(new URL(`../../../${path}`, import.meta.url), 'utf8');
        css.should.not.contain('event-modeling-grid-specification-header');
        css.should.not.contain('overflow-wrap: anywhere');
    });
    it('should keep a route-free specification title unchanged', () => {
        const document = toEventModelDocument(parse(source.replace(/ {10}stream Account[^\n]*\n(?: {12}streamId[^\n]*\n)?| {10}no stream\n/g, '')).value, 'Banking');
        const slice = document.collections[0].modules[0].features[0].slices[0];
        slice.specifications[0].name.should.equal('ReadingHistory');
        (slice.annotations === undefined).should.be.true;
    });
    it('should display opaque text ids as plain text rather than HTML entities or fabricated payload properties', () => {
        const document = toEventModelDocument(parse(source.replace('p-1:2026-10', '<month & year>')).value, 'Banking');
        const event = document.collections[0].modules[0].features[0].slices[0].specifications[0].given[0];
        event.name.should.contain('<month & year>');
        document.collections[0].modules[0].features[0].slices[0].annotations!.some(annotation => annotation.message.includes('<month & year>')).should.be.true;
        const action = toEventModelDocument(parse(source.replace('stream Account.Profile', 'stream Account.Transactions\n            streamId = "<month & year>"')).value, 'Banking')
            .collections[0].modules[0].features[0].slices[0].specifications[0].whenEvent!;
        action.name.should.contain('<month & year>');
        event.values.should.deep.equal({ stream: 5 });
    });
});
