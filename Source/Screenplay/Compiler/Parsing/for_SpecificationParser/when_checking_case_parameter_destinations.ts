// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse, parseSpecificationSource } from '../../ScreenplayCompiler';

const declarations = `module M
  feature F
    slice StateChange Receiving
      event Received
      command Receive
        id Uuid identifier
        produces Received
          for id
    slice StateChange Recording
      event Recorded
      command Record
        id Int identifier
        produces Recorded
          for id`;
const history = `
      specification Recording
        parameter receivedId Uuid
        case Prior receivedId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        given Received
          for case.receivedId
        when Record id = 42
        then Recorded
          for 42`;
const append = `
      specification Appending
        parameter id Uuid
        case One id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        when append Received
          for case.id
        then Received`;
const typeDiagnostics = (source: string) => parse(source).diagnostics.filter(diagnostic => ['PLAY0587', 'PLAY0585'].includes(diagnostic.code));

describe('when checking case parameter destinations', () => {
    it('should use the given events producer rather than the action command', () => typeDiagnostics(declarations + history).should.deep.equal([]));
    it('should type an append without a command action', () => typeDiagnostics(declarations + append).should.deep.equal([]));
    it('should reject an incompatible append parameter', () => typeDiagnostics(declarations + append.replace('parameter id Uuid', 'parameter id Int').replace('"3fa85f64-5717-4562-b3fc-2c963f66afa6"', '1')).map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0587']));
    it('should reject an optional append parameter', () => typeDiagnostics(declarations + append.replace('parameter id Uuid', 'parameter id Uuid optional')).map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0585']));
    it.each([['Decimal', 'PLAY0587'], ['Int optional', 'PLAY0585']])('should check trigger values for %s', (type, code) => {
        const source = `trigger Kick\n  amount Int\nmodule M\n  feature F\n    slice Automation S\n      specification Kicking\n        parameter value ${type}\n        case One value = 1\n        when trigger Kick\n          amount = case.value`;
        typeDiagnostics(source).map(diagnostic => diagnostic.code).should.deep.equal([code]);
    });
    it.each([
        ['trigger data', '      reaction Forward\n        when Kick\n          produces Received\n            for id', 'trigger Kick\n  id Uuid\n', true],
        ['literal reaction destination', '      reaction Forward\n        when Kick\n          produces Received\n            for "record"', 'trigger Kick\n', true],
        ['inherited event destination', '      event Prior\n      command Seed\n        id Uuid identifier\n        produces Prior\n          for id\n      reaction Forward\n        when Prior\n          produces Received', '', true],
        ['capture destination', '      capture Legacy\n        identified by id\n        append Received', '', true],
        ['unknown reaction destination', '      reaction Forward\n        when Unknown\n          produces Received\n            for missing', '', false],
        ['cyclic inheritance', '      event Prior\n      reaction Forward\n        when Prior\n          produces Received\n        when Received\n          produces Prior', '', false],
        ['ambiguous producers', '      command First\n        id Uuid identifier\n        produces Received\n          for id\n      command Second\n        id String identifier\n        produces Received\n          for id', '', false],
    ] as const)('should infer %s without guessing unavailable types', (_name, producer, prefix, known) => {
        const source = `${prefix}module M\n  feature F\n    slice Automation S\n      event Received\n${producer}\n      specification Appending\n        parameter id Int\n        case One id = 1\n        when append Received\n          for case.id\n        then Received`;
        typeDiagnostics(source).filter(diagnostic => diagnostic.code === 'PLAY0587').length.should.equal(known ? 1 : 0);
    });
    it('should use a production destination rather than an unrelated identifier property', () => {
        const source = declarations.replace('id Uuid identifier', 'id Int identifier\n        target Uuid').replace('for id\n    slice', 'for target\n    slice');
        typeDiagnostics(source + history).should.deep.equal([]);
    });
    it('should leave an undeclared event destination unknown', () => typeDiagnostics(declarations + append.replaceAll('Received', 'Unknown')).should.deep.equal([]));
    it.each([['Amount', '1'], ['Detail', '{"value":1}'], ['Status', '"future"']])('should defer unavailable standalone type %s', (type, value) => {
        const result = parseSpecificationSource(`specification Recording\n  parameter amount ${type}\n  case One amount = ${value}\n  when Record amount = case.amount`);
        result.success.should.equal(true);
        result.diagnostics.should.deep.equal([]);
    });
    it('should defer unknown concept compatibility with an intrinsic String target', () => {
        parseSpecificationSource('specification Rejecting\n  parameter reason Message\n  case One reason = "rejected"\n  then error case.reason').diagnostics.should.deep.equal([]);
    });
    it.each([['Int', '"bad"'], ['Detail[]', '1'], ['Detail', 'null']])('should keep standalone primitive and value shape checks for %s', (type, value) => {
        parseSpecificationSource(`specification Recording\n  parameter amount ${type}\n  case One amount = ${value}\n  when Record amount = case.amount`).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0583']);
    });
    it('should still reject undeclared parameter types in a complete application', () => {
        parse('module M\n  feature F\n    slice StateChange S\n      specification Recording\n        parameter amount Amount\n        case One amount = 1\n        when Record amount = case.amount').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0587']);
    });
    it('should check standalone error parameter types', () => {
        parseSpecificationSource('specification Rejecting\n  parameter reason Int\n  case One reason = 1\n  then error case.reason').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0587']);
    });
    it('should check standalone concrete values without inventing missing trigger shapes', () => {
        parseSpecificationSource('specification Kicking\n  parameter amount Int\n  case One amount = "bad"\n  when trigger Unknown\n    amount = case.amount').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0583']);
    });
    it('should leave standalone target types unknown when no declarations are available', () => {
        parseSpecificationSource('specification Kicking\n  parameter amount Decimal\n  case One amount = 1\n  when trigger Unknown\n    amount = case.amount').diagnostics.should.deep.equal([]);
    });
    it('should locate an inline assignment after a case name containing its parameter name', () => {
        const result = parseSpecificationSource('specification Recording\n  parameter amount Int\n  case amountTooLow amount = 0\n  when Record amount = case.amount');
        result.value[0].cases![0].values[0].location.column.should.equal(21);
    });
});
