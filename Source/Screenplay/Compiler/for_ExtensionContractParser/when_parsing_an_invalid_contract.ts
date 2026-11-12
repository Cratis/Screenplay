// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { parseExtensionContract } from '../ScreenplayCompiler';
import { stripeContract } from '../Conformance/extensionContractSample';

const codes = (source: string): string[] => parseExtensionContract(source).diagnostics.map(diagnostic => diagnostic.code);
const withLine = (line: string): string => stripeContract.replace('  frontend "frontend/index.js"', `  frontend "frontend/index.js"\n${line}`);

describe('when parsing an invalid contract', () => {
    it('should report a missing display name', () => expect(codes(stripeContract.replace('  display name "Stripe payments"\n', ''))).toEqual([DiagnosticCodes.MissingExtensionManifestField]));
    it('should report a missing description', () => expect(codes(stripeContract.replace('  description "Card payments with Stripe"\n', ''))).toEqual([DiagnosticCodes.MissingExtensionManifestField]));
    it('should report a missing icon', () => expect(codes(stripeContract.replace('  icon "assets/icon.svg"\n', ''))).toEqual([DiagnosticCodes.MissingExtensionManifestField]));
    it('should report a component without a placeholder', () => expect(codes(stripeContract.replace('    placeholder "placeholders/payment-form.html"\n', ''))).toEqual([DiagnosticCodes.ComponentWithoutPlaceholder]));
    it('should report a component without a kind', () => expect(codes(stripeContract.replace('    kind react\n', ''))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report an unknown component kind', () => expect(codes(stripeContract.replace('kind react', 'kind vue'))).toEqual([DiagnosticCodes.UnknownComponentKind]));
    it('should report an unknown platform', () => expect(codes(stripeContract.replace('    platform web', '    platform toaster'))).toEqual([DiagnosticCodes.UnknownComponentPlatform]));
    it('should report an unknown required platform', () => expect(codes(stripeContract.replace('requires platform web', 'requires platform toaster'))).toEqual([DiagnosticCodes.UnknownComponentPlatform]));
    it('should report a setting that is public and secret', () => expect(codes(withLine('  setting both public secret'))).toEqual([DiagnosticCodes.SettingBothPublicAndSecret]));
    it('should report an unknown setting modifier', () => expect(codes(withLine('  setting odd weird'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a component invoking an undeclared ability', () => expect(codes(stripeContract.replace('invokes CreatePaymentIntent', 'invokes Refund'))).toEqual([DiagnosticCodes.ComponentInvokesUnknownAbility]));
    it('should report a duplicate ability', () => expect(codes(withLine('  ability CreatePaymentIntent\n    amount Money'))).toEqual([DiagnosticCodes.DuplicateExtensionMember, DiagnosticCodes.DuplicateAbility].filter(code => codes(withLine('  ability CreatePaymentIntent\n    amount Money')).includes(code))));
    it('should report a duplicate trigger', () => expect(codes(withLine('  trigger PaymentSucceeded'))).toEqual([DiagnosticCodes.DuplicateExtensionMember]));
    it('should report an unknown line', () => expect(codes(withLine('  surprise 1'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a file without an extension', () => expect(codes('setting x')).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report an empty file', () => expect(codes('')).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a second extension declaration', () => expect(codes(`${stripeContract}extension Other\n`)).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed header', () => expect(codes('extension 1')).toContain(DiagnosticCodes.InvalidExtensionContract));
    it('should report a repeated one-time line', () => expect(codes(withLine('  icon "other.svg"'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed string line', () => expect(codes(withLine('  description nope'))).toContain(DiagnosticCodes.InvalidExtensionContract));
    it('should report a one-line directive with children', () => expect(codes(stripeContract.replace('  icon "assets/icon.svg"', '  icon "assets/icon.svg"\n    extra'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a bad image child', () => expect(codes(stripeContract.replace('    caption "The payment form on a checkout screen"', '    note "x"'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed image', () => expect(codes(withLine('  image nope'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed requirement', () => expect(codes(withLine('  requires nothing "1"'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed setting', () => expect(codes(withLine('  setting 1'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed trigger', () => expect(codes(withLine('  trigger 1'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed trigger field', () => expect(codes(stripeContract.replace('    paymentIntentId String\n\n  component', '    ???\n\n  component'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed component', () => expect(codes(withLine('  component 1'))).toContain(DiagnosticCodes.InvalidExtensionContract));
    it('should report an unknown component line', () => expect(codes(stripeContract.replace('    kind react', '    kind react\n    sparkle yes'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed property', () => expect(codes(stripeContract.replace('    property amount Money', '    property ???'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed raises', () => expect(codes(stripeContract.replace('    raises succeeded', '    raises 1'))).toContain(DiagnosticCodes.InvalidExtensionContract));
    it('should report a malformed invokes', () => expect(codes(stripeContract.replace('invokes CreatePaymentIntent', 'invokes 1'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
    it('should report a malformed backend', () => expect(codes(stripeContract.replace('backend runtime dotnet 9 assembly "Cratis.Extensions.Stripe.dll"', 'backend dotnet'))).toEqual([DiagnosticCodes.InvalidExtensionContract]));
});
