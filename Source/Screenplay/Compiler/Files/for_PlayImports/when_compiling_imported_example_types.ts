// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { compileApplication, parsePlacedDocuments } from '../PlayApplicationAssembly';
import { parse } from '../../ScreenplayCompiler';
import { expandEffectiveSpecificationExamples } from '../../Parsing/SpecificationCommandExamples';

const declarations = `module Billing
  feature Facts
    slice StateChange Recording
      command Record
        id String identifier
        amount Int
        produces Recorded
          for id
          amount = amount
      event Recorded
        amount Int
      readmodel Balance
        balanceId String
        amount Int
      specification Recording
        given Fact
        given readmodel View
        when Input
        then Fact amount = 20
        then readmodel View`;

const importing = `import Billing.Facts.Recording.Recorded
import Billing.Facts.Recording.Balance
import Billing.Facts.Recording.Record
import "declarations.play"
example Fact : Recorded
  amount = 10
  for "one"
example View : Balance
  balanceId = "one"
  amount = 10
example Input : Record
  id = "one"
  amount = 20`;

describe('when compiling imported example types', () => {
    let result: ReturnType<typeof compileApplication>;
    let effective: ReturnType<typeof expandEffectiveSpecificationExamples>;

    beforeEach(() => {
        result = compileApplication(new Map([
            ['application.play', importing],
            ['declarations.play', declarations]
        ]), ['application.play']);
        effective = expandEffectiveSpecificationExamples(result.value);
    });

    it('should compile the assembled application without resolution errors', () => {
        result.diagnostics.should.deep.equal([]);
    });

    it('should expand the imported event', () => {
        effective.specifications[0].effective.given[0].eventType.should.equal('Billing.Facts.Recording.Recorded');
    });

    it('should expand the imported read model', () => {
        effective.specifications[0].effective.givenReadModels[0].name.should.equal('Billing.Facts.Recording.Balance');
    });

    it('should expand the imported command', () => {
        effective.specifications[0].effective.when!.commandType.should.equal('Billing.Facts.Recording.Record');
    });
});

describe('when parsing imported example types without their declarations', () => {
    it('should defer imported type resolution', () => {
        parse(importing).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0520').should.deep.equal([]);
    });

    it.each(['Recorded', 'Billing.Facts.Recording.Recorded'])('should defer the imported name %s', type => {
        parse(`import Billing.Facts.Recording.Recorded\nexample Fact : ${type}`).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0520').should.deep.equal([]);
    });
});

describe('when parsing placed documents with imported example types', () => {
    const documents = [{ path: 'application.play', source: importing, placement: [] }];

    it('should validate unresolved imported types strictly by default', () => {
        parsePlacedDocuments(documents).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0520').length.should.equal(3);
    });

    it('should defer unresolved imported types when requested by an editor', () => {
        parsePlacedDocuments(documents, undefined, true).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0520').should.deep.equal([]);
    });
});

describe('when validating example types in a single document', () => {
    it('should report an unknown local example type even if unused', () => {
        const result = parse('example Fact : Missing', 'application.play');
        result.diagnostics.should.deep.equal([{
            severity: 'error', code: 'PLAY0520',
            arguments: { example: { kind: 'text', value: 'Fact' }, type: { kind: 'text', value: 'Missing' } },
            message: "Example 'Fact' type 'Missing' is unknown or ambiguous; qualify a declared event, command, or read model.",
            location: { path: 'application.play', line: 1, column: 1 }
        }]);
        result.success.should.equal(false);
    });

    it.each([
        'import Billing.Facts.Recording.Missing\nexample Fact : Missing\nmodule M\n  feature F\n    slice StateChange A\n      event Missing\n    slice StateChange B\n      event Missing',
        'import A.F.S.Missing\nimport B.F.S.Missing\nexample Fact : Missing'
    ])('should not defer an ambiguous type in %s', source => {
        parse(source).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0520').length.should.equal(1);
    });

    it.each([
        ['concept Shape : String', 'concept'],
        ['type Shape\n  value String', 'type'],
        ['module M\n  feature F\n    slice StateChange S\n      event Recorded\nexample Prior : Recorded', 'example']
    ])('should reject a type of the wrong kind in %s', (declaration, kind) => {
        const type = kind === 'example' ? 'Prior' : 'Shape';
        const result = parse(`import A.F.S.${type}\n${declaration}\nexample Fact : ${type}`);
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0520').map(diagnostic => diagnostic.message).should.deep.equal([
            `Example 'Fact' must name an event, command, or read model, not a ${kind}; example inheritance is not supported.`
        ]);
    });
});

describe('when an imported example type does not resolve in the assembled compilation', () => {
    it('should report the unresolved example type', () => {
        const result = compileApplication(new Map([['application.play', importing.replace('import "declarations.play"\n', '')]]));
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0520').map(diagnostic => ({ message: diagnostic.message, location: diagnostic.location })).should.deep.equal([
            { message: "Example 'Fact' type 'Recorded' is unknown or ambiguous; qualify a declared event, command, or read model.", location: { path: 'application.play', line: 4, column: 1 } },
            { message: "Example 'View' type 'Balance' is unknown or ambiguous; qualify a declared event, command, or read model.", location: { path: 'application.play', line: 7, column: 1 } },
            { message: "Example 'Input' type 'Record' is unknown or ambiguous; qualify a declared event, command, or read model.", location: { path: 'application.play', line: 10, column: 1 } }
        ]);
        result.success.should.equal(false);
    });
});
