// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_disclosing_specification_route_readiness : Specification
{
    McpAuthoringReadiness _readiness;
    SpecificationSyntax _specification;
    SliceSyntax _slice;
    ApplicationSyntax _application;

    void Establish()
    {
        _application = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateView S\n      event E\n      specification X\n        when append E\n        then E\n          no stream").Value!;
        _slice = _application.Modules.Single().Features.Single().Slices.Single();
        _specification = _slice.Specifications.Single();
    }

    void Because() => _readiness = new(_application);

    [Fact] void should_disclose_the_specification_as_syntax_only() => _readiness.SyntaxOnly(_specification).ShouldBeTrue();
    [Fact] void should_name_the_unadmitted_route_on_the_specification() => _readiness.ExecutionReadiness(_specification).ShouldContain("specification event routes (#457)");
    [Fact] void should_disclose_the_slice_as_syntax_only() => _readiness.SyntaxOnly(_slice).ShouldBeTrue();
    [Fact] void should_disclose_the_model_as_syntax_only() => _readiness.ModelSyntaxOnly.ShouldBeTrue();
    [Fact] void should_name_the_unadmitted_route_on_the_model() => _readiness.ModelExecutionReadiness.ShouldContain("specification event routes (#457)");
}
