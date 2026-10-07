// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing.for_SpecificationStreamValidator;

public class when_validating_typed_unrouted_history
{
    [Theory]
    [InlineData("given")]
    [InlineData("when append")]
    void should_refuse_an_unparseable_history_marker(string keyword)
    {
        var application = new ScreenplayCompiler().Parse($"module M\n  feature F\n    slice StateView S\n      event E\n      specification X\n        {keyword} E").Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single();
        var occurrence = keyword == "given" ? specification.Given.Single() : specification.WhenAppended!;
        occurrence = occurrence with { NoStream = new SpecificationNoStreamSyntax(occurrence.Location) };
        specification = keyword == "given" ? specification with { Given = [occurrence] } : specification with { WhenAppended = occurrence };
        application = application with { Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [specification] }] }] }] };
        var context = ParserContext.ForDiagnostics();

        ScreenplayValidator.Validate(application, context);

        context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("PLAY0547");
    }
}
