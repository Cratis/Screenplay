// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { should } from 'chai';
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
