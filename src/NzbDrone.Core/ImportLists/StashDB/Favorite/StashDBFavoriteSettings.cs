using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.StashDB.Favorite
{
    public class StashDBFavoriteSettingsValidator : StashDBSettingsBaseValidator<StashDBFavoriteSettings>
    {
        public StashDBFavoriteSettingsValidator()
        : base()
        {
            RuleFor(c => c.ApiKey)
                .NotEmpty()
                .WithMessage("Api Key must not be empty");
        }
    }

    public class StashDBFavoriteSettings : StashDBSettingsBase<StashDBFavoriteSettings>, IStashDBTagFilterSettings
    {
        protected override AbstractValidator<StashDBFavoriteSettings> Validator => new StashDBFavoriteSettingsValidator();

        [FieldDefinition(3, Label = "Favorite Filter", Type = FieldType.Select, SelectOptions = typeof(FavoriteFilter), HelpText = "Filter by favorited entity")]
        public int Filter { get; set; }

        [FieldDefinition(4, Label = "StashDBIncludedTagStashIds", HelpText = "StashDBIncludedTagStashIdsHelpText")]
        public string IncludedTags { get; set; }

        [FieldDefinition(5, Label = "StashDBExcludedTagStashIds", HelpText = "StashDBExcludedTagStashIdsHelpText")]
        public string ExcludedTags { get; set; }
    }
}
