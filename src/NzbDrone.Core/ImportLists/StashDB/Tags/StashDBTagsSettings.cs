using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.StashDB.Studio
{
    public class StashDBTagsSettingsValidator : StashDBSettingsBaseValidator<StashDBTagsSettings>
    {
        public StashDBTagsSettingsValidator()
        : base()
        {
            RuleFor(c => c.ApiKey)
                .NotEmpty()
                .WithMessage("Api Key must not be empty");
        }
    }

    public class StashDBTagsSettings : StashDBSettingsBase<StashDBTagsSettings>, IStashDBTagFilterSettings
    {
        protected override AbstractValidator<StashDBTagsSettings> Validator => new StashDBTagsSettingsValidator();

        [FieldDefinition(4, Label = "StashDBIncludedTagStashIds", HelpText = "StashDBIncludedTagStashIdsHelpText")]
        public string IncludedTags { get; set; }

        [FieldDefinition(5, Label = "StashDBExcludedTagStashIds", HelpText = "StashDBExcludedTagStashIdsHelpText")]
        public string ExcludedTags { get; set; }
    }
}
