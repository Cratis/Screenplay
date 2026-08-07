// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_that_reads_state : given.a_compiler
{
    const string Source =
        """
        module Timesheets
          feature HourRegistration
            slice StateChange StartMonth
              command StartMonth
                engagementId Uuid
                reads EngagementScope by engagementId

                produces TimesheetStarted
                  engagementId = engagementId
                  consultantId = EngagementScope.consultantId

              event TimesheetStarted
                engagementId Uuid
                consultantId Uuid

            slice StateView EngagementScopes
              projection EngagementScopes => EngagementScope
                from TimesheetStarted key engagementId
                  consultantId = consultantId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_declare_the_read_model() => Reads.ReadModel.ShouldEqual("EngagementScope");
    [Fact] void should_declare_the_key_it_reads_by() => Reads.By.ShouldEqual("engagementId");

    // The point of the declaration - a mapping fed from state, which is what dropped to an inline block before.
    [Fact] void should_map_a_produced_property_from_state() =>
        ((PathExpressionSyntax)Mapping.Source).Path.ShouldEqual("EngagementScope.consultantId");

    ReadsSyntax Reads => Command.Reads!.Single();

    PropertyMappingSyntax Mapping =>
        Command.Produces.Single().Mappings.Single(mapping => mapping.Property == "consultantId");

    CommandSyntax Command =>
        _result.Value!.Modules.Single().Features.Single().Slices.First().Commands.Single();
}
