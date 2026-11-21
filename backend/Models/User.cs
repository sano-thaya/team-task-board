
namespace TeamTaskBoard.Api.Models;

public class User
{
    public long Id { get; set; }

    public string FirebaseUid { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = "Member";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();

    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}
