// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_identifying_scheduled_attachments_under_a_non_invariant_culture : given.a_semantic_binder
{
    const string Source =
        """
        module Orders
          feature Ordering
            slice Automation Notify
              reaction Notifier
                at 08:30
                  file Reactions/Notifier.cs
        """;

    CultureInfo _previous = null!;
    SemanticImplementationRequirement _invariant = null!;
    SemanticImplementationRequirement _localized = null!;

    void Establish()
    {
        _previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        _invariant = Bind(Source).ImplementationRequirements.Single();
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");
    }

    void Because() => _localized = Bind(Source).ImplementationRequirements.Single();

    void Destroy() => CultureInfo.CurrentCulture = _previous;

    [Fact] void should_keep_the_same_requirement_id() => _localized.RequirementId.ShouldEqual(_invariant.RequirementId);
    [Fact] void should_keep_the_literal_time_separator() => _localized.Member.ShouldContain("at 08:30/");
}
