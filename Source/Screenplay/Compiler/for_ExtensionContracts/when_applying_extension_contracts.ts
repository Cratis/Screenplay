// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { applyExtensionContracts, checkExtensionSettings } from '../Parsing/ExtensionContracts';
import { parse, parseExtensionContract } from '../ScreenplayCompiler';
import { stripeContract } from '../Conformance/extensionContractSample';

const contract = parseExtensionContract(stripeContract).value!;
const contracts = new Map([['cratis/stripe', contract]]);
const model = `secret Key
  description "k"

system Payments
  extension "cratis/stripe" version 2
  setting secretKey from secret Key
  setting publishableKey = "pk"
  setting statementDescriptor = "ACME"
`;

describe('when applying extension contracts', () => {
    const application = parse(model).value;
    const applied = applyExtensionContracts(application, contracts);

    it('should give the system the contracts abilities', () => expect(applied.systems![0].abilities.map(ability => ability.name)).toEqual(['CreatePaymentIntent']));
    it('should leave the written syntax alone', () => expect(application.systems![0].abilities).toEqual([]));
    it('should place the abilities on the extension line', () => expect(applied.systems![0].abilities[0].location).toEqual(application.systems![0].extension!.location));
    it('should place nested syntax on the extension line', () => expect(applied.systems![0].abilities[0].inputs.map(input => input.location)).toEqual([application.systems![0].extension!.location, application.systems![0].extension!.location]));
    it('should change nothing without contracts', () => expect(applyExtensionContracts(application, new Map())).toBe(application));
    it('should leave a system without an extension alone', () => expect(applyExtensionContracts(parse('system S\n  ability A\n').value, contracts).systems![0].abilities.length).toBe(1));
    it('should leave a system whose contract is unknown alone', () => expect(applyExtensionContracts(application, new Map([['other', contract]])).systems![0].abilities).toEqual([]));
    it('should leave a model without systems alone', () => expect(applyExtensionContracts(parse('').value, contracts).systems).toEqual([]));
    it('should accept settings that match the contract', () => expect(checkExtensionSettings(application, contracts)).toEqual([]));
});

describe('when checking settings against a contract', () => {
    const codes = (source: string): string[] => checkExtensionSettings(parse(source).value, contracts).map(diagnostic => diagnostic.code);
    const system = (setting: string): string => `secret Key\n  description "k"\n\nsystem Payments\n  extension "cratis/stripe"\n  setting secretKey from secret Key\n  ${setting}\n`;

    it('should report an unknown setting', () => expect(codes(system('setting nothing = 1'))).toEqual([DiagnosticCodes.UnknownSystemSetting]));
    it('should report a literal on a secret setting', () => expect(codes(system('setting secretKey = "x"').replace('  setting secretKey from secret Key\n', ''))).toEqual([DiagnosticCodes.SecretSettingLiteral]));
    it('should report a secret on a public setting', () => expect(codes(system('setting publishableKey from secret Key'))).toEqual([DiagnosticCodes.SecretOnPublicSetting]));
    it('should accept a secret on a backend setting', () => expect(codes(system('setting statementDescriptor from secret Key'))).toEqual([]));
    it('should ignore a system without a contract', () => expect(codes('system S\n  extension "other"\n  setting x = 1\n')).toEqual([]));
    it('should ignore a system without an extension', () => expect(codes('system S\n  description "d"\n')).toEqual([]));
});

describe('when a system declares abilities beside an extension', () => {
    it('should be an error at the ability', () => expect(parse('system Payments\n  extension "cratis/stripe"\n  ability Charge\n    amount String\n').diagnostics.map(diagnostic => diagnostic.code)).toContain(DiagnosticCodes.SystemAbilitiesBesideExtension));
    it('should accept abilities without an extension', () => expect(parse('system Payments\n  ability Charge\n    amount String\n').diagnostics.map(diagnostic => diagnostic.code)).not.toContain(DiagnosticCodes.SystemAbilitiesBesideExtension));
});
