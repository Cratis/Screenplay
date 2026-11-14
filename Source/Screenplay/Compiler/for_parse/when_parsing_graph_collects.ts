// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { parse } from '../ScreenplayCompiler';
import { GraphQuerySyntax } from '../Syntax/Graphs';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';

const header = [
    'concept PersonId : Uuid', 'concept GroupId : Uuid', 'concept Name : String',
    'type Membership', '  id GroupId', '  since DateTime optional',
    'type Member', '  id PersonId', '  name Name',
];
const slice = [
    'module M', '  feature F', '    slice StateView S',
    '      graph G', '        node Person', '          personId PersonId key', '          name Name',
    '        node Group', '          groupId GroupId key',
    '        edge MEMBER_OF source Person target Group', '          since DateTime optional',
    '      readmodel PersonWithGroups', '        id PersonId key', '        groups Membership[]',
    '      readmodel GroupWithLead', '        id GroupId key', '        lead Member optional',
];
const valid = [
    'query People => PersonWithGroups[] graph G', '  filter groupId GroupId', '  match person:Person', '  where group.groupId = $groupId',
    '  return', '    id = person.personId', '    groups = collect person -link:MEMBER_OF-> group:Group', '      id = group.groupId', '      since = link.since',
];
const single = [
    'query Groups => GroupWithLead[] graph G', '  match group:Group',
    '  return', '    id = group.groupId', '    lead = collect group <-MEMBER_OF- member:Person', '      id = member.personId', '      name = member.name',
];
const compile = (body: string[], types: string[] = header) => parse([...types, ...slice, ...body.map(line => `      ${line}`)].join('\n'));
const codes = (body: string[], types: string[] = header) => compile(body, types).diagnostics.filter(diagnostic => diagnostic.severity !== 'information').map(diagnostic => diagnostic.code);
const queryOf = (body: string[]): GraphQuerySyntax => compile(body).value.modules[0].features[0].slices[0].graphQueries![0];
const replaced = (from: string, to: string) => valid.map(line => line.replace(from, to));

describe('when parsing graph collects', () => {
    it('should accept a list collect', () => { codes(valid).should.deep.equal([]); });
    it('should accept a single row collect over an incoming edge', () => { codes(single).should.deep.equal([]); });
    it('should read the collect apart from the scalar returns', () => {
        const query = queryOf(valid);
        query.returns.map(item => item.property).should.deep.equal(['id']);
        query.collects![0].should.include({ property: 'groups', from: 'person' });
        query.collects![0].hop.should.include({ type: 'MEMBER_OF', outgoing: true, alias: 'link' });
        query.collects![0].hop.node.should.include({ alias: 'group', label: 'Group' });
        query.collects![0].returns.map(item => `${item.property}=${item.alias}.${item.source}`).should.deep.equal(['id=group.groupId', 'since=link.since']);
    });
    it('should read an incoming edge', () => { queryOf(single).collects![0].hop.outgoing.should.equal(false); });
    it('should leave a query without collects without the member', () => {
        (queryOf(['query Q => PersonWithGroups[] graph G', '  match person:Person', '  return', '    id = person.personId']).collects === undefined).should.equal(true);
    });
    it('should visit the collect and its row', () => {
        const kinds: string[] = [];
        new (class extends ScreenplaySyntaxWalker { override visitNode(node: { kind: string }): void { kinds.push(node.kind); } })().visitGraphQuery(queryOf(valid));
        kinds.should.include('GraphCollectSyntax');
    });
    it('should report a collect without a pattern', () => { codes(replaced('collect person -link:MEMBER_OF-> group:Group', 'collect')).should.include(DiagnosticCodes.InvalidGraphCollect); });
    it('should report a collect of two edges', () => { codes(replaced('-> group:Group', '-> group:Group <-MEMBER_OF- other:Person')).should.include(DiagnosticCodes.InvalidGraphCollect); });
    it('should report a collect from another alias', () => { codes(replaced('collect person', 'collect stranger')).should.include(DiagnosticCodes.InvalidGraphCollect); });
    it('should report a malformed row', () => { codes(replaced('      since = link.since', '      since link.since')).should.include(DiagnosticCodes.InvalidGraphCollect); });
    it('should report a row read from the root', () => { codes(replaced('id = group.groupId', 'id = person.personId')).should.include(DiagnosticCodes.InvalidGraphCollect); });
    it('should report a collect with no row', () => { codes(valid.slice(0, 7)).should.include(DiagnosticCodes.InvalidGraphCollect); });
    it('should report a collect into a required single row', () => {
        const source = [...header, ...slice.map(line => line.replace('lead Member optional', 'lead Member')), ...single.map(line => `      ${line}`)].join('\n');
        parse(source).diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.GraphCollectTargetMismatch);
    });
    it('should report a row property the type lacks', () => { codes([...valid, '      extra = group.groupId']).should.include(DiagnosticCodes.GraphCollectTargetMismatch); });
    it('should report a row property assigned twice', () => { codes([...valid, '      id = group.groupId']).should.include(DiagnosticCodes.GraphCollectTargetMismatch); });
    it('should report a missing row property', () => { codes(single.slice(0, 6)).should.include(DiagnosticCodes.GraphCollectTargetMismatch); });
    it('should report a row property of another type', () => { codes(replaced('id = group.groupId', 'id = link.since')).should.include(DiagnosticCodes.GraphCollectTargetMismatch); });
    it('should report a nested collect', () => { codes([...valid, '      more = collect group <-MEMBER_OF- other:Person', '        id = other.personId']).should.include(DiagnosticCodes.NestedGraphCollect); });
    it('should report a match that follows an edge', () => { codes(replaced('  match person:Person', '  match person:Person -MEMBER_OF-> other:Group')).should.include(DiagnosticCodes.GraphCollectQueryShape); });
    it('should report a condition on a collected edge', () => { codes(replaced('where group.groupId = $groupId', 'where link.since = "2026-01-01T00:00:00Z"')).should.include(DiagnosticCodes.GraphCollectQueryShape); });
    it('should report a scalar return from a collected node', () => { codes([...valid.slice(0, 6), '    id = group.groupId', ...valid.slice(6)]).should.include(DiagnosticCodes.GraphCollectQueryShape); });
    it('should report a collect against the edge direction', () => { codes(replaced('-link:MEMBER_OF->', '<-link:MEMBER_OF-')).should.include(DiagnosticCodes.GraphQueryEdgeMismatch); });
    it('should report a property assigned by a return and a collect', () => { codes([...valid.slice(0, 6), '    groups = person.personId', ...valid.slice(6)]).should.include(DiagnosticCodes.GraphQueryReturnMismatch); });
    it('should report a collect into a property the read model lacks', () => { codes(replaced('groups = collect', 'nope = collect')).should.include(DiagnosticCodes.GraphQueryReturnMismatch); });
});
