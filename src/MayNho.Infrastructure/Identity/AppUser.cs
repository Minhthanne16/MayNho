using MayNho.Domain;
using Microsoft.AspNetCore.Identity;

namespace MayNho.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    public UserProfile? Profile { get; set; }
    public UserPreference? Preference { get; set; }
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
