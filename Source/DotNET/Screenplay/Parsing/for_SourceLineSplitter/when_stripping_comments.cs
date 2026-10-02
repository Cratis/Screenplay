// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_SourceLineSplitter;

public class when_stripping_comments
{
    // Keep these cases aligned with Monaco's for_documentContext/when_stripping_comments.ts.
    [Theory]
    [InlineData("value = path // comment", "value = path")]
    [InlineData("""value = "https://example" // comment""", "value = \"https://example\"")]
    [InlineData("""value = "escaped \" // retained" // comment""", "value = \"escaped \\\" // retained\"")]
    [InlineData("""value = "ending \\" // comment""", "value = \"ending \\\\\"")]
    [InlineData("value = `https://example` // comment", "value = `https://example`")]
    [InlineData("""value = `a " // retained` // comment""", """value = `a " // retained`""")]
    [InlineData("""value = "a ` // retained" // comment""", "value = \"a ` // retained\"")]
    [InlineData("""value = "unclosed // retained""", """value = "unclosed // retained""")]
    [InlineData("value = `unclosed // retained", "value = `unclosed // retained")]
    [InlineData("value = path # not a comment", "value = path # not a comment")]
    [InlineData("value = 'single // comment", "value = 'single")]
    void should_keep_string_and_template_contents(string source, string expected) => SourceLineSplitter.Split(source).Single().Content.ShouldEqual(expected);
}
