// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const stripeContract = `extension Stripe
  display name "Stripe payments"
  description "Card payments with Stripe"
  icon "assets/icon.svg"
  image "assets/checkout.png"
    caption "The payment form on a checkout screen"
  image "assets/settings.png"
  requires stage "^5"
  requires platform web
  activation on demand idle after 10 minutes

  setting publishableKey public
  setting secretKey secret
  setting statementDescriptor

  ability CreatePaymentIntent idempotent
    amount Money
    orderId String
    returns
      clientSecret String

  trigger PaymentSucceeded
    paymentIntentId String

  component PaymentForm
    kind react
    platform web
    placeholder "placeholders/payment-form.html"
    property amount Money
    raises succeeded
      paymentIntentId String
    invokes CreatePaymentIntent

  backend runtime dotnet 9 assembly "Cratis.Extensions.Stripe.dll"
  frontend "frontend/index.js"
`;
