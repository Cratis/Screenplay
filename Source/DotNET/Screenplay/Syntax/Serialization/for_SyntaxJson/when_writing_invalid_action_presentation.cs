// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_writing_invalid_action_presentation : Specification
{
    IEnumerable<Diagnostic> _diagnostics;
    JsonElement _wire;
    Exception? _exception;

    void Because()
    {
        var parsed = ScreenplayCompiler.Parse("screen Broken\n  action Retry for item\n    presentation placement \"\"");
        _diagnostics = parsed.Diagnostics;
        _exception = Catch.Exception(() => _wire = SyntaxJson.Serialize(parsed.Value!));
    }

    [Fact] void should_keep_the_authoring_diagnostic() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidActionPresentation);
    [Fact] void should_serialize_the_partial_document_without_throwing() => _exception.ShouldBeNull();
    [Fact] void should_preserve_the_erroneous_value() => _wire.GetProperty("screens")[0].GetProperty("directives")[0].GetProperty("presentation")[0].GetProperty("value").GetString().ShouldEqual(string.Empty);
    [Fact] void should_reject_the_invalid_value_when_reading() => Catch.Exception(() => SyntaxJson.Deserialize(_wire)).ShouldBeOfExactType<InvalidSyntaxJson>();
}
