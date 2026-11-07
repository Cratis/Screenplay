// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_InputSurfaceCompleteness;

public class when_a_feature_hook_names_the_full_slice_path : given.a_model
{
    void Establish() => Compile("""
        layout AppShell
          category Shell
          content

          outlet topbarActions of actions

          arrangement flow
            row
              content grow

        module Shop
          feature Customers
            outlet topbarActions extends
              button register title "Register" slice Shop.Customers.RegisterCustomer

            slice StateChange RegisterCustomer
              command RegisterCustomer
                name String

              screen RegisterCustomer
                title "Register customer"
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.InputSurfaces]));

    [Fact] void should_count_the_hook_as_the_issuer() => Findings.ShouldBeEmpty();
}
