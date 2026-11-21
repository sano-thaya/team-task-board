namespace TeamTaskBoard.Api.Models;

public class ProjectMember
{
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public long UserId { get; set; }
    public User User { get; set; } = null!;
}
