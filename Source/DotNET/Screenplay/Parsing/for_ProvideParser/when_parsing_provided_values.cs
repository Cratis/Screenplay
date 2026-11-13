// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ProvideParser;

public class when_parsing_provided_values : Specification
{
    const string Source =
        """
        provide region String from "Nordics"
        provide caller String[] from $context.identity.roles
        module Fleet
          provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId
          provide entitlements String from host
          feature Quality
            provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId
            slice StateView Vessels
              provide limit Int
                file Limits.cs
              provide score Int
                ```csharp
                return 1;
                ```
              query VesselByKey => Vessel optional
                by vesselId Uuid
              readmodel Vessel
                vesselId Uuid
              event VesselRegistered
                vesselId Uuid
              projection Vessels => Vessel
                from VesselRegistered key vesselId
                  vesselId = vesselId
        """;

    CompilationResult<ApplicationSyntax> _result;
    ApplicationSyntax _roundTrip;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = compiler.Compile(Source);
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _roundTrip = compiler.Compile(_printed).Value!;
    }

    [Fact] void should_compile_without_findings() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_record_application_values() => _result.Value!.Provides.Select(provide => provide.Name).ShouldContainOnly("region", "caller");
    [Fact] void should_record_a_literal_source() => _result.Value!.Provides.First().Source.ShouldBeOfExactType<ValueProvideSourceSyntax>();
    [Fact] void should_record_a_host_source() => _result.Value!.Modules.Single().Provides.Last().Source.ShouldBeOfExactType<HostProvideSourceSyntax>();
    [Fact] void should_record_a_query_with_its_key() => ((QueryProvideSourceSyntax)_result.Value!.Modules.Single().Provides.First().Source).By.Count().ShouldEqual(1);
    [Fact] void should_record_an_optional_value() => _result.Value!.Modules.Single().Provides.First().Type.IsOptional.ShouldBeTrue();
    [Fact] void should_record_a_file_source() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Provides.First().Source.ShouldBeOfExactType<FileProvideSourceSyntax>();
    [Fact] void should_record_a_code_source() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Provides.Last().Source.ShouldBeOfExactType<CodeProvideSourceSyntax>();
    [Fact] void should_preserve_syntax_on_printing() => SyntaxJson.StructurallyEqual(_result.Value!, _roundTrip).ShouldBeTrue();
    [Fact] void should_print_the_query_source() => _printed.ShouldContain("provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId");
}
