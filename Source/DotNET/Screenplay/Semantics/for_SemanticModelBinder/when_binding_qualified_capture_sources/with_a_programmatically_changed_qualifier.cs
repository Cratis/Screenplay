// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_qualified_capture_sources;

public class with_a_programmatically_changed_qualifier : given.a_capture_source
{
    ApplicationSyntax _syntax;
    SourceLocation _location;
    CompilationResult<SemanticCompilation> _result;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Compile(Source("Products.Catalog.Facts.ProductPublished"));
        parsed.Success.ShouldBeTrue();
        var application = parsed.Value;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var capture = slice.Captures.Single();
        var source = capture.Source;
        var setting = source.Settings.Single();
        _location = setting.Location;
        var changed = capture with { Source = source with { Settings = [setting with { Value = "Wrong.ProductPublished" }] } };
        _syntax = application with { Modules = [module with { Features = [feature with { Slices = [slice with { Captures = [changed] }] }] }] };
    }

    void Because() => _result = Bind(_syntax);

    [Fact] void should_fail_public_binder_admission() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_bind_the_event_by_its_short_name() => _result.Value.ShouldBeNull();
    [Fact] void should_report_one_unknown_event() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownEvent);
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_preserve_the_setting_location() => _result.Diagnostics.Single().Location.ShouldEqual(_location);
    [Fact] void should_preserve_the_unknown_event_message() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown event 'Wrong.ProductPublished' - declare its contract before classifying its usage.");
}
