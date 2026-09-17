// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_LanguageService.given;

namespace Cratis.Screenplay.for_LanguageService;

public class when_comparing_keywords_against_the_parsers : Specification
{
    IReadOnlySet<string> _dispatched;
    IReadOnlySet<string> _known;
    List<string> _missing;

    void Establish()
    {
        _dispatched = LanguageServiceKeywords.Dispatched();
        _known = LanguageServiceKeywords.KnownToTheLanguageService();
    }

    void Because() => _missing = [.. _dispatched.Where(keyword => !_known.Contains(keyword)).Order(StringComparer.Ordinal)];

    // Without these two, the comparison passes by reading nothing at all - a moved file or a changed
    // `case` shape would silently turn this into a check of the empty set against the empty set.
    [Fact] void should_find_the_constructs_the_parsers_dispatch_on() => _dispatched.Count.ShouldBeGreaterThan(20);
    [Fact] void should_find_the_keywords_the_language_service_knows() => _known.Count.ShouldBeGreaterThan(50);

    [Fact] void should_carry_a_keyword_for_every_construct_the_parsers_dispatch_on() => Report().ShouldEqual(string.Empty);

    string Report() =>
        _missing.Count == 0
            ? string.Empty
            : $"The Monaco language service has no keyword for {_missing.Count} construct(s) the parsers " +
              $"dispatch on: {string.Join(", ", _missing)}. Add them to constructKeywords or clauseKeywords " +
              "in Source/Screenplay/Monaco/screenplay-language/language.ts.";
}
