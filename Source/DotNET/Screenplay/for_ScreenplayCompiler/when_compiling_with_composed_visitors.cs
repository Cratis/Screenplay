// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_with_composed_visitors : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView List
              projection List => ListReadModel
                from InvoiceRegistered
                  status = "draft"
        """;

    CompilationResult<string> _result;

    void Because() => _result = _compiler.Compile(Source, new application_visitor(new module_visitor(new feature_visitor(new slice_visitor(new projection_visitor())))));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_compose_the_narrower_visitors_into_the_broader_ones() => _result.Value.ShouldEqual("Invoicing/Invoices/List[List => ListReadModel]");

    class projection_visitor : IProjectionSyntaxVisitor<string>
    {
        public string Visit(ProjectionSyntax syntax) => $"{syntax.Name} => {syntax.ReadModel}";
    }

    class slice_visitor(IProjectionSyntaxVisitor<string> projections) : ISliceSyntaxVisitor<string>
    {
        public string Visit(SliceSyntax syntax) => syntax.Projection is null ? syntax.Name : $"{syntax.Name}[{projections.Visit(syntax.Projection)}]";
    }

    class feature_visitor(ISliceSyntaxVisitor<string> slices) : IFeatureSyntaxVisitor<string>
    {
        public string Visit(FeatureSyntax syntax) => $"{syntax.Name}/{string.Join('/', syntax.Slices.Select(slices.Visit))}";
    }

    class module_visitor(IFeatureSyntaxVisitor<string> features) : IModuleSyntaxVisitor<string>
    {
        public string Visit(ModuleSyntax syntax) => $"{syntax.Name}/{string.Join('/', syntax.Features.Select(features.Visit))}";
    }

    class application_visitor(IModuleSyntaxVisitor<string> modules) : IApplicationSyntaxVisitor<string>
    {
        public string Visit(ApplicationSyntax syntax) => string.Join(';', syntax.Modules.Select(modules.Visit));
    }
}
