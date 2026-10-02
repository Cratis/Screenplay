// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayGlob;

public class when_matching_patterns : Specification
{
    [Fact] void should_resolve_relative_to_the_importing_folder() => PlayGlob.Resolve("Ordering/Ordering.play", "Orders/*.play").ShouldEqual("Ordering/Orders/*.play");
    [Fact] void should_climb_with_dot_dot() => PlayGlob.Resolve("Ordering/Orders/Orders.play", "../../Shared/x.play").ShouldEqual("Shared/x.play");
    [Fact] void should_match_any_depth_with_a_double_star() => PlayGlob.IsMatch("Ordering/**/*.play", "Ordering/Orders/Deep/PlaceOrder.play").ShouldBeTrue();
    [Fact] void should_match_no_folder_with_a_double_star() => PlayGlob.IsMatch("Ordering/**/*.play", "Ordering/Ordering.play").ShouldBeTrue();
    [Fact] void should_keep_a_single_star_within_a_folder() => PlayGlob.IsMatch("Ordering/*.play", "Ordering/Orders/PlaceOrder.play").ShouldBeFalse();
    [Fact] void should_match_one_character_with_a_question_mark() => PlayGlob.IsMatch("Slice?.play", "Slice1.play").ShouldBeTrue();
    [Fact] void should_only_match_play_files() => PlayGlob.IsMatch("**/*", "Ordering/notes.md").ShouldBeFalse();
    [Fact] void should_take_the_fixed_folder_before_the_first_wildcard() => PlayGlob.StaticFolder("Ordering/Orders/**/*.play").ShouldEqual("Ordering/Orders");
    [Fact] void should_tell_a_pattern_from_a_path() => PlayGlob.HasWildcard("Ordering/Ordering.play").ShouldBeFalse();
}
