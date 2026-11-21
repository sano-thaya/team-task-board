namespace TeamTaskBoard.Api.Models;

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public enum TaskStatus
{
    ToDo,
    InProgress,
    Completed
}

public class TaskItem
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long AssignedUserId { get; set; }
    public User AssignedUser { get; set; } = null!;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;
    public DateTime? DueDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
