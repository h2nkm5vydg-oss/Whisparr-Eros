using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.StashDB.Studio
{
    public class StashDBPerformerSettingsValidator : StashDBSettingsBaseValidator<StashDBStudioSettings>
    {
        public StashDBPerformerSettingsValidator()
        : base()
        {
            RuleFor(c => c.ApiKey)
                .NotEmpty()
                .WithMessage("Api Key must not be empty");

            RuleFor(c => c.Studios)
                .NotEmpty()
                .WithMessage("Studios StashIDs must not be empty");
        }
    }

    public class StashDBStudioSettings : StashDBSettingsBase<StashDBStudioSettings>, IStashDBTagFilterSettings
    {
        protected override AbstractValidator<StashDBStudioSettings> Validator => new StashDBPerformerSettingsValidator();

        [FieldDefinition(3, Label = "Studios StashIDs", HelpText = "Enter Studios StashIDs, comma seperated")]
        public string Studios { get; set; }

        [FieldDefinition(4, Label = "StashDBIncludedTagStashIds", HelpText = "StashDBIncludedTagStashIdsHelpText")]
        public string IncludedTags { get; set; }

        [FieldDefinition(5, Label = "StashDBExcludedTagStashIds", HelpText = "StashDBExcludedTagStashIdsHelpText")]
        public string ExcludedTags { get; set; }

        [FieldDefinition(6, Label = "Only Favorite Performers", Type = FieldType.Checkbox, HelpText = "Filter by favorite performers")]
        public bool OnlyFavoritePerformers { get; set; }
    }
}
