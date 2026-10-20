// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_feature_toolbar_linking_an_unknown_command : Specification
{
    const string Source =
        """
        module Shop
          feature Orders
            toolbar actions
              item ship action ShipOrder
              item place action PlaceOrder
              button place title "Duplicate" slice PlaceOrder
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_warn_about_the_command_the_feature_does_not_declare() => _result.Diagnostics.Where(_ => _.Code == DiagnosticCodes.FeatureToolbarCommandNotFound).Select(_ => _.Message).ShouldContain(message => message.Contains("'ship'") && message.Contains("ShipOrder"));
    [Fact] void should_accept_the_command_it_declares() => _result.Diagnostics.Count(_ => _.Code == DiagnosticCodes.FeatureToolbarCommandNotFound).ShouldEqual(1);
    [Fact] void should_report_the_id_used_twice() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(DiagnosticCodes.DuplicateToolbarButton);
}
