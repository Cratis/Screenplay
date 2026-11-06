// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { GraphQuerySyntax } from '../Syntax/Graphs';

const declarations = [
    'concept PersonId : Uuid', 'concept GroupId : Uuid', 'concept Name : String', 'module M', '  feature F', '    slice StateView S',
    '      graph G', '        node Person', '          personId PersonId key', '          name Name',
    '        node Group', '          groupId GroupId key', '          title Name',
    '        edge MEMBER_OF source Person target Group', '          since DateTime optional',
    '        edge LEADS source Person target Group',
    '      readmodel Membership', '        personId PersonId key', '        person Name', '        group GroupId', '        since DateTime optional',
    '      readmodel Counted', '        groupId GroupId key', '        title Name',
];
const valid = [
    'query MembersOf => Membership[] graph G', '  filter groupId GroupId',
    '  match person:Person -link:MEMBER_OF-> group:Group', '  where group.groupId = $groupId',
    '  return', '    personId = person.personId', '    person = person.name', '    group = group.groupId', '    since = link.since',
];
const compile = (...body: string[]) => parse([...declarations, ...body.map(line => `      ${line}`)].join('\n'));
const codes = (...body: string[]) => compile(...body).diagnostics.filter(diagnostic => diagnostic.severity !== 'info').map(diagnostic => diagnostic.code);
const replaced = (from: string, to: string) => valid.map(line => line.replace(from, to));
const queryOf = (...body: string[]): GraphQuerySyntax => compile(...body).value.modules[0].features[0].slices[0].graphQueries![0];

describe('when parsing graph queries', () => {
    it('should accept a valid query', () => { codes(...valid).should.deep.equal([]); });
    it('should read the declaration', () => {
        const query = queryOf(...valid);
        query.name.should.equal('MembersOf');
        query.graph.should.equal('G');
        query.returnType.isCollection.should.equal(true);
        query.matches[0].hops[0].should.include({ type: 'MEMBER_OF', outgoing: true, alias: 'link' });
        query.returns.map(item => item.property).should.deep.equal(['personId', 'person', 'group', 'since']);
    });
    it('should read an observable query with an optional match, by, a literal, authorize and a description', () => {
        const query = queryOf('query Q => observable Counted graph G', '  description "How many"', '  by groupId GroupId', '  authorize Admin',
            '  match group:Group', '  optional match group:Group <-LEADS- leader:Person', '  where group.groupId = $groupId and group.title = "Ops"',
            '  return', '    groupId = group.groupId', '    title = group.title');
        query.isObservable.should.equal(true);
        query.description!.should.equal('How many');
        query.by!.name.should.equal('groupId');
        query.authorize!.should.not.equal(null);
        query.matches[1].isOptional.should.equal(true);
        query.matches[1].hops[0].outgoing.should.equal(false);
        query.conditions.length.should.equal(2);
    });
    it('should visit every node of a query', () => {
        const kinds = new Set<string>();
        new class extends ScreenplaySyntaxWalker { override visitNode(node: { kind: string }): void { kinds.add(node.kind); super.visitNode(node as never); } }().visitApplication(compile(...valid).value);
        ['GraphQuerySyntax', 'GraphMatchSyntax', 'GraphPatternNodeSyntax', 'GraphPatternHopSyntax', 'GraphConditionSyntax', 'GraphReturnSyntax'].forEach(kind => kinds.has(kind).should.equal(true, kind));
    });

    const cases: [string, string[], string[]][] = [
        ['no match', ['query Q => Membership[] graph G', '  return', '    person = person.name'], ['PLAY0878']],
        ['no return', ['query Q => Membership[] graph G', '  match person:Person -MEMBER_OF-> group:Group'], ['PLAY0878']],
        ['only an optional match', ['query Q => Membership[] graph G', '  optional match person:Person -MEMBER_OF-> group:Group', '  return', '    person = person.name'], ['PLAY0878']],
        ['a pattern it cannot read', ['query Q => Membership[] graph G', '  match 123', '  return', '    person = person.name'], ['PLAY0878']],
        ['a hop it cannot read', ['query Q => Membership[] graph G', '  match person:Person -MEMBER_OF- group:Group', '  return', '    person = person.name'], ['PLAY0878']],
        ['an optional match without an edge', ['query Q => Membership[] graph G', '  match person:Person -MEMBER_OF-> group:Group', '  optional match person:Person', '  return', '    person = person.name'], ['PLAY0878']],
        ['an empty where', ['query Q => Membership[] graph G', '  match person:Person -MEMBER_OF-> group:Group', '  where', '  return', '    person = person.name'], ['PLAY0878']],
        ['a return it cannot read', ['query Q => Membership[] graph G', '  match person:Person -MEMBER_OF-> group:Group', '  return', '    person person.name'], ['PLAY0878']],
        ['a return with no block', ['query Q => Membership[] graph G', '  match person:Person -MEMBER_OF-> group:Group', '  return person.name'], ['PLAY0878']],
        ['an unknown directive', ['query Q => Membership[] graph G', '  nonsense'], ['PLAY0878']],
        ['a condition value it cannot read', ['query Q => Membership[] graph G', '  match person:Person -MEMBER_OF-> group:Group', '  where group.groupId = ???', '  return', '    person = person.name'], ['PLAY0878']],
        ['a condition that is not an equality', ['query Q => Membership[] graph G', '  match person:Person -MEMBER_OF-> group:Group', '  where group.groupId > $x', '  return', '    person = person.name'], ['PLAY0885']],
        ['or', ['query Q => Membership[] graph G', '  filter x GroupId', '  match person:Person -MEMBER_OF-> group:Group', '  where group.groupId = $x or group.groupId = $y', '  return', '    person = person.name'], ['PLAY0885']],
    ];
    for (const [name, body, expected] of cases) {
        it(`should report ${name}`, () => { codes(...body).should.include.members(expected); });
    }

    const refused: [string, string[], string][] = [
        ['an unknown graph', replaced('graph G', 'graph Nope'), 'PLAY0879'],
        ['an unknown label', replaced('group:Group', 'group:Nope'), 'PLAY0879'],
        ['an unknown edge', replaced('link:MEMBER_OF', 'link:NOPE'), 'PLAY0879'],
        ['an unknown property', replaced('person.name', 'person.nope'), 'PLAY0879'],
        ['an unknown alias', replaced('person.name', 'stranger.name'), 'PLAY0879'],
        ['an edge in the wrong direction', replaced('-link:MEMBER_OF->', '<-link:MEMBER_OF-'), 'PLAY0880'],
        ['a duplicate alias', replaced('group:Group', 'person:Group'), 'PLAY0884'],
        ['an undeclared argument', replaced('$groupId', '$other'), 'PLAY0882'],
        ['an argument of another type', replaced('filter groupId GroupId', 'filter groupId String'), 'PLAY0882'],
        ['a missing required return', valid.filter(line => !line.includes('person = person.name')), 'PLAY0881'],
        ['a return the read model lacks', [...valid, '    extra = person.name'], 'PLAY0881'],
        ['a return of another type', replaced('person = person.name', 'person = person.personId'), 'PLAY0881'],
        ['a duplicate return', [...valid, '    person = person.name'], 'PLAY0881'],
        ['an optional source for a required property', ['query Q => Membership[] graph G', '  match person:Person', '  optional match person:Person -link:MEMBER_OF-> group:Group',
            '  return', '    personId = person.personId', '    person = person.name', '    group = group.groupId'], 'PLAY0883'],
    ];
    for (const [name, body, expected] of refused) {
        it(`should refuse ${name}`, () => { codes(...body).should.include(expected); });
    }
    it('should accept an optional source for an optional property', () => {
        codes('query Q => Membership[] graph G', '  match person:Person', '  optional match person:Person -link:MEMBER_OF-> group:Group',
            '  return', '    personId = person.personId', '    person = person.name', '    group = person.personId', '    since = link.since').should.not.include('PLAY0883');
    });
    it('should share a start node between paths without a duplicate alias', () => {
        codes('query Q => Membership[] graph G', '  match person:Person -MEMBER_OF-> group:Group', '  match person:Person -LEADS-> led:Group',
            '  return', '    personId = person.personId', '    person = person.name', '    group = led.groupId').should.not.include('PLAY0884');
    });
});
