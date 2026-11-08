// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_summarizing_slices;

public class when_a_slice_is_added_and_another_removed : given.a_model_with_dependent_slices
{
    const string Archive = """
            slice StateChange Archive
              command Archive
                projectId Uuid identifier
                produces Archived
                  for projectId
              event Archived
        """;

    const string Close = """
            slice StateChange Close
              command Close
                projectId Uuid identifier
                produces Closed
                  for projectId
              event Closed
        """;

    void Establish()
    {
        _byAddress = true;
        ChangeSource(Source.Replace(Archive, Close, StringComparison.Ordinal));
    }

    void Because() => Compare();

    [Fact] void should_report_the_added_slice() => Slice("Projects.Registration.Close").Changes.ShouldContainOnly(SliceChangeKind.Added);
    [Fact] void should_have_no_address_before_for_the_added_slice() => Slice("Projects.Registration.Close").BeforeAddress.ShouldBeNull();
    [Fact] void should_report_the_removed_slice() => Slice("Projects.Registration.Archive").Changes.ShouldContainOnly(SliceChangeKind.Removed);
    [Fact] void should_have_no_address_after_for_the_removed_slice() => Slice("Projects.Registration.Archive").AfterAddress.ShouldBeNull();
    [Fact] void should_attribute_the_removed_declarations_to_the_removed_slice() => Slice("Projects.Registration.Archive").Declarations.ShouldContain(declaration => declaration.Kind == "Event" && declaration.BeforeAddress == "Projects.Registration.Archive.Archived");
    [Fact] void should_report_nothing_else() => _result.Slices.Slices.Count.ShouldEqual(2);
}
