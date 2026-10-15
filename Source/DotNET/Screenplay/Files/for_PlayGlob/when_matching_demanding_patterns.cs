// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayGlob;

public class when_matching_demanding_patterns : Specification
{
    static readonly string _manyStars = $"{string.Concat(Enumerable.Repeat("a*", 200))}b.play";
    static readonly string _manyDoubleStars = $"{string.Concat(Enumerable.Repeat("**/", 40))}x.play";
    static readonly string _tenThousandDoubleStars = $"{string.Concat(Enumerable.Repeat("**/", 10_000))}x.play";
    static readonly string _deepPath = $"{string.Join('/', Enumerable.Repeat("folder", 200))}/y.play";

    [Fact] void should_refuse_a_segment_that_many_stars_cannot_match() => PlayGlob.IsMatch(_manyStars, $"{new string('a', 5000)}.play").ShouldBeFalse();
    [Fact] void should_match_a_segment_with_many_stars() => PlayGlob.IsMatch(_manyStars, $"{new string('a', 5000)}b.play").ShouldBeTrue();
    [Fact] void should_refuse_a_deep_path_that_many_double_stars_cannot_match() => PlayGlob.IsMatch(_manyDoubleStars, _deepPath).ShouldBeFalse();
    [Fact] void should_match_a_deep_path_with_many_double_stars() => PlayGlob.IsMatch(_manyDoubleStars, _deepPath.Replace("y.play", "x.play")).ShouldBeTrue();
    [Fact] void should_match_a_shallow_path_against_ten_thousand_double_stars() => PlayGlob.IsMatch(_tenThousandDoubleStars, "x.play").ShouldBeTrue();
    [Fact] void should_match_anything_beneath_a_trailing_double_star() => PlayGlob.IsMatch("Ordering/**", "Ordering/Orders/Deep/PlaceOrder.play").ShouldBeTrue();
    [Fact] void should_need_a_folder_beneath_a_trailing_double_star() => PlayGlob.IsMatch("Ordering/**", "Ordering.play").ShouldBeFalse();
    [Fact] void should_not_let_an_inner_double_star_take_an_empty_folder() => PlayGlob.IsMatch("Ordering/**/x.play", "Ordering//x.play").ShouldBeFalse();
    [Fact] void should_match_characters_literally() => PlayGlob.IsMatch("Order+[s].play", "Order+[s].play").ShouldBeTrue();
    [Fact] void should_match_case_sensitively() => PlayGlob.IsMatch("Ordering/*.play", "ordering/x.play").ShouldBeFalse();
    [Fact] void should_need_exactly_one_character_for_a_question_mark() => PlayGlob.IsMatch("Slice?.play", "Slice.play").ShouldBeFalse();
}
