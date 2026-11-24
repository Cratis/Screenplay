// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_recovering_misplaced_otherwise_blocks : Specification
{
    const string Source = """
        on click
          otherwise
            execute First
          when item.status == "open"
            execute Guarded
          otherwise hidden
            execute Ignored
          otherwise
            execute Last
        """;

    ParserContext _context;
    InteractionBindingSyntax _binding;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _binding = InteractionParser.ParseInlineBehavior(_context, _context.Reader.TakeSignificant())!.Bindings.Single();

    [Fact] void should_keep_the_alternative_after_the_early_fallback() => ((ExecuteCommandActionSyntax)_binding.Alternatives.Single().Actions.Single()).Command.ShouldEqual("Guarded");
    [Fact] void should_replace_the_early_fallback_with_the_last_one() => ((ExecuteCommandActionSyntax)_binding.Otherwise!.Actions.Single()).Command.ShouldEqual("Last");
    [Fact] void should_keep_the_last_fallback_location() => _binding.Otherwise!.Location.ShouldEqual(new SourceLocation(8, 3));
    [Fact] void should_not_create_plain_actions() => _binding.Actions.ShouldBeEmpty();

    [Fact]
    void should_report_each_misplaced_block() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.MisplacedActionOtherwise, DiagnosticCodes.MisplacedActionOtherwise, DiagnosticCodes.MisplacedActionOtherwise]);

    [Fact]
    void should_keep_the_diagnostic_messages() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(
        ["A 'when' alternative must precede 'otherwise'", "An interaction permits one final block-form 'otherwise'", "An interaction permits one final block-form 'otherwise'"]);

    [Fact]
    void should_keep_the_diagnostic_locations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location.Line).ShouldEqual([4, 6, 8]);
}
