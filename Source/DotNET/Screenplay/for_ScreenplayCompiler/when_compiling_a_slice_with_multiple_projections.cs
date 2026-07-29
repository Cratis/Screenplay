// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_slice_with_multiple_projections : given.a_compiler
{
    const string Source =
        """
        module CustomerPortal
          feature Report
            slice StateView CustomerPortalReport
              event ReportGenerated
                reportId Uuid

              event TokenRevoked
                reportId Uuid

              projection PortalReport => PortalReport
                from ReportGenerated
                  reportId = reportId

              projection RevokedCustomerPortalToken => RevokedCustomerPortalToken
                from TokenRevoked
                  reportId = reportId
        """;

    CompilationResult<ApplicationSyntax> _result;
    SliceSyntax _slice;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _slice = _result.Value!.Modules.Single().Features.Single().Slices.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_both_projections() => _slice.Projections.Count().ShouldEqual(2);
    [Fact] void should_keep_the_view_model() => _slice.Projections.First().ReadModel.ShouldEqual("PortalReport");
    [Fact] void should_keep_the_companion_model() => _slice.Projections.Last().ReadModel.ShouldEqual("RevokedCustomerPortalToken");
}
