// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_event_tag_expectations : given.a_semantic_binder
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        produces E\n          value = value\n      event E\n        value String\n      specification X\n        when C value = \"x\"\n        then E value = \"x\"\n";
    ExecutableSemanticModel _older;
    ExecutableSemanticModel _tagged;
    ExecutableSemanticModel _empty;
    ExecutableSemanticModel _unchecked;

    void Because()
    {
        _older = Bind(Source).Value!.Model;
        _tagged = Bind(Source + "          tag audit\n          tag audit\n").Value!.Model;
        _empty = Bind(Source + "          no tags\n").Value!.Model;
        var application = _tagged.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single();
        var updated = slice with { Specifications = [specification with { ThenEvents = [specification.ThenEvents.Single() with { ExpectedTags = null }] }] };
        _unchecked = ExecutableSemanticModel.Create(_older.LanguageVersion, _older.SemanticVersion,
            application with { Modules = [module with { Features = [feature with { Slices = [updated] }] }] });
    }

    [Fact] void should_keep_older_versions_without_the_new_form() => _older.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_select_v55_for_a_tag_expectation() => _tagged.SemanticVersion.ShouldEqual(SemanticVersion.V55);
    [Fact] void should_select_v55_for_no_tags() => _empty.SemanticVersion.ShouldEqual(SemanticVersion.V55);
    [Fact] void should_preserve_order_and_duplicates() => _tagged.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().ThenEvents.Single().ExpectedTags!.Value.ToArray().ShouldEqual(new[] { "audit", "audit" });
    [Fact] void should_leave_older_output_byte_identical() => SemanticModelSerializer.Serialize(_unchecked).ShouldEqual(SemanticModelSerializer.Serialize(_older));
    [Fact] void should_not_write_expected_tags_in_older_output() => System.Text.Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_older)).ShouldNotContain("expectedTags");
}
