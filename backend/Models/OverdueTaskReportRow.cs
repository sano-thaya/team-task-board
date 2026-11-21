namespace TeamTaskBoard.Api.Models;

/// <summary>Read-only row returned by the MySQL overdue report query.</summary>
public sealed class OverdueTaskReportRow
{
    public long TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long AssignedUserId { get; set; }
    public string AssignedUserName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int Status { get; set; }
    public DateTime DueDateUtc { get; set; }
    public long OverdueTaskCount { get; set; }
}
