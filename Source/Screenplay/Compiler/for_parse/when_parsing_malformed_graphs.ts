// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { LineReader } from '../Parsing/LineReader';
import { ParserContext } from '../Parsing/ParserContext';
import { parseProjection } from '../Parsing/ProjectionParser';
import { SourceLine } from '../Parsing/SourceLine';
import { parse } from '../ScreenplayCompiler';

const slice = (...body: string[]) => [
    'concept PersonId : Uuid', 'module M', '  feature F', '    slice StateView S',
    '      event Added', '        groupId PersonId', '      event Removed',
    ...body.map(line => `      ${line}`)].join('\n');

const baseGraph = ['graph G', '  node Person', '    id PersonId key', '  node Group', '    id PersonId key',
    '  edge LINKS source Person target Group', '    since DateTime'];

const codes = (...body: string[]) => parse(slice(...body)).diagnostics.map(diagnostic => diagnostic.code);

describe('when parsing malformed graphs', () => {
    const cases: [string, string[], string[]][] = [
        ['graph header', ['graph', '  node A', '    id PersonId key'], ['PLAY0862']],
        ['graph body line', ['graph G', '  node A', '    id PersonId key', '  bogus'], ['PLAY0862']],
        ['graph without nodes', ['graph G'], ['PLAY0862']],
        ['node header', ['graph G', '  node', '    id PersonId key'], ['PLAY0862']],
        ['edge header', ['graph G', '  node A', '    id PersonId key', '  edge broken'], ['PLAY0862']],
        ['bad property', ['graph G', '  node A', '    id PersonId key', '    ???'], ['PLAY0862']],
        ['edge key', ['graph G', '  node A', '    id PersonId key', '  edge E source A target A', '    id PersonId key'], ['PLAY0862']],
        ['generated modifier', ['graph G', '  node A', '    id PersonId key', '    other String generated'], ['PLAY0862']],
        ['duplicates', [...baseGraph, 'graph G', '  node Person', '    id PersonId key', '  node Person', '    id PersonId key', '    id PersonId'], ['PLAY0874','PLAY0874','PLAY0874']],
        ['duplicate edge and property', [...baseGraph, '  edge LINKS source Person target Group', '    since DateTime', '    since DateTime'], ['PLAY0874','PLAY0874']],
        ['bad key counts', ['graph G', '  node A', '    a PersonId', '  node B', '    a PersonId key', '    b PersonId key'], ['PLAY0864','PLAY0864']],
        ['bad types', ['graph G', '  node A', '    id PersonId key', '    other Missing', '  edge E source A target A', '    other Missing'], ['PLAY0863','PLAY0863']],
        ['bad endpoints and identity', ['graph G', '  node A', '    id PersonId key', '  edge E source Q target Z identified by nope'], ['PLAY0865','PLAY0865','PLAY0871']],
        ['projection', [...baseGraph, 'projection P => graph', 'projection P2 => graph G', 'projection P3 => graph Missing', '  node A'], ['PLAY0873','PLAY0873','PLAY0873','PLAY0866']],
        ['empty projection', [...baseGraph, 'projection P => graph G'], ['PLAY0873']],
        ['projection body', [...baseGraph, 'projection P => graph G', '  bogus', '  children x', '  description "d"', '  node Person', '    from Added key groupId', '  node Person', '    bogus', '    every', '      automap', '      since = 1'], ['PLAY0873','PLAY0872','PLAY0873','PLAY0872','PLAY0873','PLAY0874','PLAY0868']],
        ['node and edge blocks', [...baseGraph, 'projection P => graph G', '  node', '  edge', '  node Person', '  edge LINKS', '  node Nope', '    from Added key groupId', '  edge Nope', '    from Added source a target b'], ['PLAY0873','PLAY0873','PLAY0873','PLAY0873','PLAY0867','PLAY0867']],
        ['from forms', [...baseGraph, 'projection P => graph G', '  node Person', '    from Added', '    from Added source a target b', '    from Unknown key groupId', '      id = groupId', '      nope = groupId', '      ???', '  edge LINKS', '    from Added key groupId', '    from Added source a target b identified by c'], ['PLAY0873','PLAY0873','PLAY0075','PLAY0873','PLAY0166','PLAY0868','PLAY0871']],
        ['remove forms', [...baseGraph, 'projection P => graph G', '  node Person', '    from Added key groupId', '    remove with', '    remove with Removed key groupId', '      body', '    remove with Removed source a target b', '    remove with Removed key groupId detach', '  node Group', '    from Added key groupId', '  edge LINKS', '    from Added source a target b', '    remove with Removed key a', '    remove with Removed key a detach', '    remove with Removed source a target b detach', '    remove with Removed', '    remove with Removed source a target b identified by c'], ['PLAY0873','PLAY0873','PLAY0873','PLAY0873','PLAY0869','PLAY0873','PLAY0869','PLAY0873','PLAY0870','PLAY0870','PLAY0870','PLAY0871']],
        ['identified edge', ['graph G', '  node A', '    id PersonId key', '  edge E source A target A identified by tag', '    tag String', 'projection P => graph G', '  edge E', '    from Added source groupId target groupId', '    remove with Removed source groupId target groupId identified by groupId'], ['PLAY0873']],
        ['spec steps', [...baseGraph, 'specification S1', '  given graph G', '    has', '    no node Person "a"', '    has node Person', '    has node Person "a"', '      id = "a"', '      nope = 1', '    has edge LINKS from "a"', '    has edge LINKS from "a" to "b"', '      nope = 1', '    has edge LINKS from "a" to "b" identified by "c"', '    has edge Nope from "a" to "b"', '    has node Nope "a"', '  then graph Missing', '    has node A "a"', '    no node Person "a"', '      id = "a"', '    no edge LINKS from "a" to "b"', '      x = 1', '    has widget Person "a"'], ['PLAY0875','PLAY0875','PLAY0875','PLAY0875','PLAY0875','PLAY0875','PLAY0875','PLAY0868','PLAY0868','PLAY0876','PLAY0876','PLAY0876']],
    ];
    for (const [name, body, expected] of cases) {
        it(`should report diagnostics for ${name}`, () => {
            codes(...body).should.deep.equal(expected);
        });
    }
});

describe('when parsing graph details', () => {
    it('should accept a graph description and documentation', () => {
        const result = parse(slice('graph G', '  description "Who knows whom"', '  documentation', '    ```markdown', '    Some notes', '    ```',
            '  node A', '    id PersonId key'));
        result.diagnostics.should.deep.equal([]);
    });

    it('should reject a second graph description and an unfenced documentation', () => {
        const result = parse(slice('graph G', '  description "One"', '  description "Two"', '  documentation', '  node A', '    id PersonId key'));
        result.diagnostics.length.should.be.greaterThan(0);
    });

    it('should reject a graph projection handed to the plain projection parser', () => {
        const line: SourceLine = { number: 1, raw: 'projection P => graph G', indent: 0, content: 'projection P => graph G', startOffset: 0 };
        const context = new ParserContext(new LineReader([line]));
        const result = parseProjection(context, line);
        result.name.should.equal('projection');
        context.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0877']);
    });
});
