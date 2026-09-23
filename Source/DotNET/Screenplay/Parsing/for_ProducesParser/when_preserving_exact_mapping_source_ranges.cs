// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ProducesParser;

public class when_preserving_exact_mapping_source_ranges : Specification
{
    const string Source = "module Projects\r\n  feature Registration\r\n    slice StateChange Register\r\n      command Register\r\n        produces Registered\r\n          name  =  café  // café 🍰\r\n";
    PropertyMappingSyntax _mapping = null!;
    string _sourceText = null!;

    void Because()
    {
        var parsed = new ScreenplayCompiler().Parse(Source, "Registration.play");
        _mapping = parsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().Mappings.Single();
        var location = _mapping.SourceLocation!;
        _sourceText = Source.Split('\n')[location.Line - 1].Substring(location.Column - 1, _mapping.SourceLength!.Value);
    }

    [Fact] void should_capture_only_the_rhs_identifier() => _sourceText.ShouldEqual("café");
    [Fact] void should_preserve_the_source_path() => _mapping.SourceLocation!.Path.ShouldEqual("Registration.play");
    [Fact] void should_preserve_the_existing_mapping_location() => _mapping.Location.Column.ShouldEqual(11);
    [Fact] void should_preserve_the_existing_expression_location() => _mapping.Source.Location.ShouldEqual(_mapping.Location);
}
#endif
