// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { should } from 'chai';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';

should();

const source = `policy IsBuyer
  require role "buyer"
concept Money : Decimal
  validate
    decimals <= 2 message "Money has at most two decimals"
module Shop
  feature Basket
    slice StateChange PutInBasket
      command PutInBasket
        basketId Uuid identifier
        surcharge Decimal
        validate
          surcharge decimals <= 3
        produces LineAdded
          for basketId
      event LineAdded
        unitPrice Money
        quantity Int
    slice StateView BasketLookup
      readmodel Basket
        basketId Uuid key
        total Money optional
      projection BasketProjection => Basket
        from LineAdded
          add total by unitPrice * quantity
          subtract total by unitPrice * 0.5
          add total by unitPrice
      screen BasketScreen
        toolbar actions
          button put title "Put in basket" slice Shop.Basket.PutInBasket
            authorize IsBuyer
          button plain title "Plain" slice Shop.Basket.PutInBasket
`;

describe('when parsing the acme commerce admissions', () => {
    let result: ReturnType<typeof parse>;
    let application: ApplicationSyntax;
    beforeEach(() => {
        result = parse(source);
        application = result.value as ApplicationSyntax;
    });

    const slice = (name: string) => application.modules[0].features[0].slices.find(value => value.name === name)!;
    const mappings = () => (slice('BasketLookup').projections![0].blocks[0] as unknown as { mappings: { kind: string; value?: { path?: string }; factor?: { path?: string; value?: unknown } }[] }).mappings;

    it('should parse without errors', () => result.diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.be.empty);
    it('should read the decimal places rule on a concept', () => (application.concepts![0].validations![0] as unknown as { rules: { rule: string }[] }).rules[0].rule.should.equal('DecimalPlaces'));
    it('should read the decimal places rule on a command property', () => (slice('PutInBasket').commands![0].validations[0] as unknown as { rules: { rule: string }[] }).rules[0].rule.should.equal('DecimalPlaces'));
    it('should read the product factor on add', () => (mappings()[0].factor?.path ?? '').should.equal('quantity'));
    it('should read the product factor on subtract', () => mappings()[1].factor?.value?.should.equal(0.5));
    it('should leave a plain add without a factor', () => (mappings()[2].factor === undefined).should.equal(true));
    it('should read the authorize line under a toolbar button', () => {
        const toolbar = (slice('BasketLookup').screens![0].directives as unknown as { kind: string; buttons?: { id: string; authorize?: unknown }[] }[]).find(directive => directive.kind === 'ScreenToolbarSyntax')!;
        (toolbar.buttons![0].authorize !== undefined).should.equal(true);
        (toolbar.buttons![1].authorize === undefined).should.equal(true);
    });
});

describe('when parsing a toolbar button with something other than authorize under it', () => {
    let result: ReturnType<typeof parse>;
    beforeEach(() => {
        result = parse(source.replace('          button plain title "Plain" slice Shop.Basket.PutInBasket\n', '          button plain title "Plain" slice Shop.Basket.PutInBasket\n            item x action PutInBasket\n            authorize IsBuyer\n            authorize IsBuyer\n'));
    });

    it('should refuse the other line and the second authorize', () => result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.InvalidToolbarButton).length.should.equal(2));
});

for (const button of [
    'button',
    'button b title "T" slice Shop.Basket.PutInBasket unknown value',
    'button b title "T" title "U"',
    'button b title plain',
    'button b title "T" slice "Quoted"',
    'button b title "T" emphasis loud',
    'button b slice Shop.Basket.PutInBasket',
    'button b title "T" icon plus emphasis quiet',
]) {
    describe(`when parsing the toolbar button ${button}`, () => {
        let result: ReturnType<typeof parse>;
        beforeEach(() => {
            result = parse(source.replace('          button plain title "Plain" slice Shop.Basket.PutInBasket\n', `          ${button}\n`));
        });

        it('should read it the way the C# compiler does', () => result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.InvalidToolbarButton).length.should.equal(button.endsWith('quiet') ? 0 : 1));
    });
}
