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
        specification.when!.name.should.contain('stream Account.Profile');
        specification.when!.values.should.deep.equal({ streamId: 6 });
        specification.thenEvents[0].name.should.contain('stream Account.Partitioned');
        specification.thenEvents[0].name.should.contain('streamId = 202610');
        specification.thenEvents[1].name.should.contain('no stream');
        specification.thenEvents[2].name.should.equal('Recorded');
        specification.given[0].name.should.contain('PLAY0268');
    });
    it('should render every route in the header even when given and then events are owned', () => {
        const document = toEventModelDocument(parse(source.replace('slice StateView History', 'slice StateChange History')).value, 'Banking');
        problems_the_board_finds_in(document).should.deep.equal([]);
        const slice = document.collections[0].modules[0].features[0].slices[0];
        slice.specifications[0].given[0].eventId.should.equal(slice.events[0].id);
        slice.specifications[0].thenEvents[0].eventId.should.equal(slice.events[0].id);
        const board = readEventModelDocument(document);
        const specification = board.collections[0].modules[0].features[0].slices[0].specifications[0];
        const markup = renderToStaticMarkup(createElement(SpecificationHeader, {
            name: specification.name, collapsed: specification.collapsed, runState: SpecificationRunState.Idle,
        }));
        markup.should.contain('given 1: Recorded');
        markup.should.contain('stream Account.Transactions; streamId = &quot;p-1:2026-10&quot;');
        markup.should.contain('when append: Recorded');
        markup.should.contain('stream Account.Profile');
        markup.should.contain('then 1: Recorded');
        markup.should.contain('stream Account.Partitioned; streamId = 202610');
        markup.should.contain('then 2: Recorded — no stream');
        markup.should.contain('PLAY0268');
    });
    it.each(['McpApp/board.css', 'VSCodeExtension/Webview/board.css'])('should keep the route-bearing header readable in %s', path => {
        const css = readFileSync(new URL(`../../../${path}`, import.meta.url), 'utf8');
        const header = css.match(/\.screenplay-board-view \.event-modeling-grid-specification-header\s*\{([^}]+)\}/)![1];
        header.should.contain('height: auto');
        const label = css.match(/\.screenplay-board-view \.event-modeling-grid-specification-header-label \.event-modeling-grid-editable-label\s*\{([^}]+)\}/)![1];
        label.should.contain('white-space: normal');
        label.should.contain('overflow-wrap: anywhere');
        label.should.contain('overflow: visible');
    });
    it('should keep a route-free specification title unchanged', () => {
        const document = toEventModelDocument(parse(source.replace(/ {10}stream Account[^\n]*\n(?: {12}streamId[^\n]*\n)?| {10}no stream\n/g, '')).value, 'Banking');
        document.collections[0].modules[0].features[0].slices[0].specifications[0].name.should.equal('ReadingHistory');
    });
    it('should display opaque text ids as plain text rather than HTML entities or fabricated payload properties', () => {
        const document = toEventModelDocument(parse(source.replace('p-1:2026-10', '<month & year>')).value, 'Banking');
        const event = document.collections[0].modules[0].features[0].slices[0].specifications[0].given[0];
        event.name.should.contain('<month & year>');
        document.collections[0].modules[0].features[0].slices[0].specifications[0].name.should.contain('<month & year>');
        const action = toEventModelDocument(parse(source.replace('stream Account.Profile', 'stream Account.Transactions\n            streamId = "<month & year>"')).value, 'Banking')
            .collections[0].modules[0].features[0].slices[0].specifications[0].when!;
        action.name.should.contain('<month & year>');
        event.values.should.deep.equal({ stream: 5 });
    });
});
