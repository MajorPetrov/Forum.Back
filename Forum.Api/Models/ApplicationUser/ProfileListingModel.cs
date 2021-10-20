using System.Collections.Generic;

namespace Forum.Models.ApplicationUser
{
    public class ProfileListingModel
    {
        public IEnumerable<ProfileModel> Profiles { get; set; }
    }
}