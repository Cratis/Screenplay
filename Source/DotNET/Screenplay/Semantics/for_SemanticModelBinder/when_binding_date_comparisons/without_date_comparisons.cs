// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_date_comparisons;

public class without_date_comparisons : given.a_date_comparison
{
    ExecutableSemanticModel[] _models;

    void Because() => _models = [.. new[] { "Date", "DateTime" }.Select(primitive => Bind(Model(primitive, "id not empty")).Value!.Model)];

    [Fact] void should_not_promote_date_shaped_commands_without_comparisons() => _models.All(model => model.SemanticVersion == SemanticVersion.V2).ShouldBeTrue();
    [Fact] void should_keep_the_existing_language_version() => _models.All(model => model.LanguageVersion == LanguageVersion.V2).ShouldBeTrue();
}
