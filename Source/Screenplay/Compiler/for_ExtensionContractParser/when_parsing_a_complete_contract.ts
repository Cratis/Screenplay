// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parseExtensionContract } from '../ScreenplayCompiler';
import { stripeContract } from '../Conformance/extensionContractSample';

describe('when parsing a complete contract', () => {
    const result = parseExtensionContract(stripeContract);
    const contract = result.value!;

    it('should report nothing', () => expect(result.diagnostics).toEqual([]));
    it('should succeed', () => expect(result.success).toBe(true));
    it('should read the manifest', () => expect([contract.displayName, contract.description, contract.icon]).toEqual(['Stripe payments', 'Card payments with Stripe', 'assets/icon.svg']));
    it('should read the images with their captions', () => expect(contract.images.map(image => image.caption)).toEqual(['The payment form on a checkout screen', null]));
    it('should read the requirements', () => expect(contract.requirements.map(requirement => `${requirement.kind} ${requirement.value}`)).toEqual(['stage ^5', 'platform web']));
    it('should read the settings with their exposure', () => expect(contract.settings.map(setting => setting.exposure)).toEqual(['public', 'secret', 'backend']));
    it('should read the abilities', () => expect(contract.abilities.map(ability => [ability.name, ability.effectClass, ability.returns.length])).toEqual([['CreatePaymentIntent', 'idempotent', 1]]));
    it('should read the triggers', () => expect(contract.triggers[0].payload.map(field => field.name)).toEqual(['paymentIntentId']));
    it('should read the component', () => {
        const component = contract.components[0];
        expect([component.kind, component.placeholder, component.platforms, component.properties.map(property => property.name), component.signals.map(signal => signal.name), component.invokes])
            .toEqual(['react', 'placeholders/payment-form.html', ['web'], ['amount'], ['succeeded'], ['CreatePaymentIntent']]);
    });
    it('should read the backend and frontend', () => expect([contract.backend?.runtime, contract.backend?.version, contract.backend?.assembly, contract.frontend]).toEqual(['dotnet', '9', 'Cratis.Extensions.Stripe.dll', 'frontend/index.js']));
    it('should read the activation', () => expect([contract.activation?.always, contract.activation?.idleAmount, contract.activation?.idleUnit]).toEqual([false, 10, 'minute']));
});
