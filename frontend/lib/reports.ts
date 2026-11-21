import { auth } from "./firebase";
import type { TaskPriority, TaskStatus } from "./tasks";

export type OverdueTask = { id: number; title: string; description: string; priority: TaskPriority; status: TaskStatus; dueDateUtc: string };
export type OverdueTaskMember = { assignedUserId: number; assignedUserName: string; overdueTaskCount: number; tasks: OverdueTask[] };
export type OverdueTaskReport = { projectId: number; totalOverdueTaskCount: number; members: OverdueTaskMember[] };

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5159";

export async function getOverdueTaskReport(projectId: number): Promise<OverdueTaskReport> {
  const user = auth.currentUser;
  const token = user ? await user.getIdToken() : null;
  if (!token) throw new Error("Please sign in again to view reports.");
  const response = await fetch(`${apiUrl}/api/projects/${projectId}/reports/overdue-tasks`, { headers: { Authorization: `Bearer ${token}` } });
  if (!response.ok) { const body = await response.json().catch(() => null) as { message?: string } | null; throw new Error(body?.message ?? "The overdue task report could not be loaded."); }
  return response.json() as Promise<OverdueTaskReport>;
}
