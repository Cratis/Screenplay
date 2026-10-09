// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reporting_route_readiness : Specification
{
    const string Source = """
        eventsource Account
          identifier String
          stream Ledger
            streamId String
        module M
          feature F
            slice StateChange S
              command C
                id String identifier
                period String
                stream Account.Ledger
                  streamId = period
                produces event E
                  for id
              specification X
                when C
                  id = "one"
                  period = "October"
                then E
        """;

    ApplicationSyntax _application;
    McpAuthoringReadiness _readiness;
    CommandSyntax Command => _application.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    void Establish() => _application = new ScreenplayCompiler().Parse(Source).Value!;

    void Because() => _readiness = new(_application);

    [Fact] void should_report_source_declarations_as_admitted() => _readiness.SyntaxOnly(_application.EventSources.Single()).ShouldBeFalse();
    [Fact] void should_report_the_route_as_admitted() => _readiness.SyntaxOnly(Command).ShouldBeFalse();
    [Fact] void should_report_the_model_as_admitted() => _readiness.ModelSyntaxOnly.ShouldBeFalse();
    [Fact] void should_have_no_unadmitted_feature_message() => _readiness.ModelExecutionReadiness.ShouldBeNull();

    [Fact]
    void should_follow_the_specification_routes_join()
    {
        var application = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateView S\n      event E\n      specification X\n        when append E\n        then E\n          no stream").Value!;
        new McpAuthoringReadiness(application).ModelSyntaxOnly.ShouldEqual(!SemanticModelBinder.SpecificationRoutesJoin);
    }

    [Fact]
    void should_follow_the_composite_stream_ids_join()
    {
        var application = new ScreenplayCompiler().Parse("eventsource Account\n  stream Ledger\n    streamId\n      bucket String\n      period String").Value!;
        new McpAuthoringReadiness(application).ModelSyntaxOnly.ShouldEqual(!SemanticModelBinder.CompositeStreamIdsJoin);
    }

    [Fact]
    void should_still_refuse_a_property_path()
    {
        var application = new ScreenplayCompiler().Parse("type Period\n  value String\n" + Source.Replace("period String", "period Period", StringComparison.Ordinal).Replace("streamId = period", "streamId = period.value", StringComparison.Ordinal)).Value!;
        var readiness = new McpAuthoringReadiness(application);
        readiness.ModelSyntaxOnly.ShouldBeTrue();
        readiness.ModelExecutionReadiness.ShouldContain("property-path stream id mappings");
    }
}
