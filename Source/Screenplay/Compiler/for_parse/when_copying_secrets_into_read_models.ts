// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { parse } from '../ScreenplayCompiler';

const concepts = 'concept ApiKey : String secret\nconcept BankAccount : String pii secret\nconcept Email : String pii\nconcept Label : String\n';
const slice = 'module M\n  feature F\n    slice StateView S\n';
const model = (eventType: string, modelType: string, mappings: string) =>
    concepts + slice +
    `      readmodel Row\n        id String\n        token ${modelType}\n      event Recorded\n        token ${eventType}\n` +
    '      projection Keys => Row\n        from Recorded\n' + mappings;
const nestedModel = (parentKind: string, innerKind: string, root: string, parent: string, inner: string) =>
    concepts +
    'type Entry\n  id String\n  token ApiKey\n' +
    `type Group\n  id String\n  entries Entry${innerKind === 'children' ? '[]' : ''}\n` +
    slice +
    `      readmodel Row\n        id String\n        groups Group${parentKind === 'children' ? '[]' : ''}\n` +
    '      event Recorded\n        id String\n        token ApiKey\n      event GroupRecorded\n        id String\n' +
    `      projection Keys => Row\n        ${root}\n` +
    `        ${parentKind} groups${parentKind === 'children' ? ' identified by id' : ''}\n          ${parent}\n          from GroupRecorded\n` +
    `          ${innerKind} entries${innerKind === 'children' ? ' identified by id' : ''}\n` +
    (inner.length > 0 ? `            ${inner}\n` : '') +
    '            from Recorded\n';
const findings = (source: string) => {
    const result = parse(source);
    result.success.should.be.true;
    return result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.SecretConceptProjected);
};

describe('when copying secrets into read models', () => {
    it('should catalogue the warning', () => DiagnosticCodes.SecretConceptProjected.should.equal('PLAY1142'));

    it('should warn for an explicit mapping into a secret property', () => {
        const [finding, ...rest] = findings(model('ApiKey', 'ApiKey', '          token = token\n'));
        rest.should.deep.equal([]);
        finding.message.should.equal("Projection 'Keys' copies secret concept 'ApiKey' into read model property 'Row.token' - the value becomes part of the read model and its query results");
        finding.severity.should.equal('warning');
        finding.location.line.should.equal(15);
    });

    it('should warn for an explicit mapping from a secret event property into a plain property', () =>
        findings(model('ApiKey', 'String', '          token = token\n')).map(finding => finding.message).should.deep.equal(["Projection 'Keys' copies secret concept 'ApiKey' into read model property 'Row.token' - the value becomes part of the read model and its query results"]));

    it('should warn when AutoMap copies a secret property', () => {
        const [finding, ...rest] = findings(model('ApiKey', 'ApiKey', ''));
        rest.should.deep.equal([]);
        finding.location.line.should.equal(14);
    });

    it('should count a personal secret as secret', () => findings(model('BankAccount', 'BankAccount', '')).length.should.equal(1));

    it('should warn when a reducer builds a secret property', () => {
        const [finding, ...rest] = findings(concepts + slice + '      readmodel Row\n        id String\n        token ApiKey\n      event Recorded\n        token ApiKey\n      reducer Fold => Row\n        on Recorded\n');
        rest.should.deep.equal([]);
        finding.message.should.equal("Reducer 'Fold' copies secret concept 'ApiKey' into read model property 'Row.token' - the value becomes part of the read model and its query results");
        finding.location.line.should.equal(13);
    });

    it('should warn for a secret copied into a child element', () =>
        findings(concepts + 'type Entry\n  id String\n  token ApiKey\n' + slice + '      readmodel Row\n        id String\n        entries Entry[]\n      event Recorded\n        id String\n        token ApiKey\n      projection Keys => Row\n        children entries identified by id\n          from Recorded\n')
            .map(finding => finding.message).should.deep.equal(["Projection 'Keys' copies secret concept 'ApiKey' into read model property 'Row.entries.token' - the value becomes part of the read model and its query results"]));

    for (const parentKind of ['children', 'nested']) {
        for (const innerKind of ['children', 'nested']) {
            for (const [root, parent, inner, count] of [
                ['automap', 'no automap', '', 1],
                ['no automap', 'automap', '', 0],
                ['automap', 'automap', 'no automap', 0],
                ['no automap', 'no automap', 'automap', 1],
            ] as const) {
                it(`should resolve ${parentKind}/${innerKind} with projection '${root}', parent '${parent}', inner '${inner || 'inherit'}'`, () => {
                    const result = findings(nestedModel(parentKind, innerKind, root, parent, inner));
                    result.length.should.equal(count);
                    if (count > 0) result[0].message.should.contain("into read model property 'Row.groups.entries.token'");
                });
            }
        }
    }

    it('should not warn for a plain concept', () => findings(model('Label', 'Label', '          token = token\n')).should.deep.equal([]));

    it('should not warn for a personal concept that is not secret', () => findings(model('Email', 'Email', '          token = token\n')).should.deep.equal([]));

    it('should not warn when automap is off and nothing maps the secret', () =>
        findings(model('ApiKey', 'ApiKey', '').replace('      projection Keys => Row\n', '      projection Keys => Row\n        no automap\n')).should.deep.equal([]));

    for (const source of ['$eventSourceId', '"redacted"']) {
        it(`should not warn for ${source}`, () =>
            findings(model('ApiKey', 'ApiKey', `          token = ${source}\n`).replace('        token ApiKey\n      projection', '        other ApiKey\n      projection')).should.deep.equal([]));
    }

    it('should not warn without a projection or reducer', () => findings(concepts + slice + '      readmodel Row\n        id String\n        token ApiKey\n').should.deep.equal([]));
});
