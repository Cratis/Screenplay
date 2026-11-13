// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { keywordDocs } from '../keyword-docs';
import { validateLines } from '../validation';

const screen = ['system Payments', '  extension "cratis/stripe" version 2', '', 'module M', '  feature F', '    slice StateChange ConfirmPayment',
    '      event PaymentConfirmed', '        paymentIntentId String', '      command ConfirmPayment', '        paymentIntentId String', '        produces PaymentConfirmed',
    '    slice StateView Checkout', '      screen Checkout', '        component Payments.PaymentForm payment', '          property amount = "10"',
    '          on succeeded', '            execute ConfirmPayment', '              with paymentIntentId from signal.paymentIntentId'];

describe('when placing an extension component', () => {
    it('should explain the component and the signal values', () => expect(['component', 'signal'].every(keyword => (keywordDocs as Record<string, string>)[keyword] !== undefined)).toBe(true));
    it('should accept a component of a system, its signal handler and the signal value it reads', () => expect(validateLines(screen)).toEqual([]));
});
