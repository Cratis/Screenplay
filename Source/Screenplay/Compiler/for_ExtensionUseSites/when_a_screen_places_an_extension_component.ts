// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { checkExtensionUseSites } from '../Parsing/ExtensionUseSites';
import { parse, parseExtensionContract } from '../ScreenplayCompiler';
import { stripeContract } from '../Conformance/extensionContractSample';

const place = (body: string): string => `concept Money : String

system Payments
  extension "cratis/stripe" version 2

ui profile Web
  target platform web
  packages scene.web

module M
  feature F
    slice StateChange ConfirmPayment
      event PaymentConfirmed
        paymentIntentId String
      command ConfirmPayment
        paymentIntentId String
        produces PaymentConfirmed
    slice StateView Checkout
      screen Checkout
${body.split('\n').map(line => `        ${line}`).join('\n')}
`;

const codes = (body: string, source = stripeContract): string[] => {
    const contracts = new Map([['cratis/stripe', parseExtensionContract(source).value!]]);
    const parsed = parse(place(body));
    expect(parsed.diagnostics.filter(diagnostic => diagnostic.severity === 'error').map(diagnostic => `${diagnostic.code} ${diagnostic.message}`)).toEqual([]);
    return checkExtensionUseSites(parsed.value, contracts).map(diagnostic => diagnostic.code);
};

describe('when a screen places an extension component', () => {
    it('should accept a declared property, signal and signal value', () => expect(codes('component Payments.PaymentForm payment\n  property amount = "10"\n  on succeeded\n    execute ConfirmPayment\n      with paymentIntentId from signal.paymentIntentId')).toEqual([]));
    it('should report an unknown component', () => expect(codes('component Payments.Missing payment')).toContain(DiagnosticCodes.UnknownExtensionComponent));
    it('should report an unknown property', () => expect(codes('component Payments.PaymentForm payment\n  property nope = "x"')).toContain(DiagnosticCodes.UnknownExtensionComponentProperty));
    it('should report an unknown signal', () => expect(codes('component Payments.PaymentForm payment\n  on exploded\n    notify info "x"')).toContain(DiagnosticCodes.UnknownExtensionSignal));
    it('should report a value the signal does not carry', () => expect(codes('component Payments.PaymentForm payment\n  on succeeded\n    execute ConfirmPayment\n      with paymentIntentId from signal.nope')).toContain(DiagnosticCodes.UnknownExtensionSignalValue));
    it('should report a platform the component does not support', () => expect(codes('component Payments.PaymentForm payment', stripeContract.replace('    platform web\n', '    platform ios\n'))).toContain(DiagnosticCodes.ExtensionComponentPlatformUnsupported));
});
