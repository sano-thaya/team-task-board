import { auth } from "./firebase";

export type TaskPriority = "Low" | "Medium" | "High";
export type TaskStatus = "ToDo" | "InProgress" | "Completed";
export type Task = { id: number; projectId: number; title: string; description: string; assignedUser: { id: number; displayName: string; email: string }; priority: TaskPriority; status: TaskStatus; dueDateUtc: string | null; createdAtUtc: string; updatedAtUtc: string };
export type TaskListResponse = { items: Task[]; page: number; pageSize: number; totalCount: number; totalPages: number };
const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5159";
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const user = auth.currentUser; const token = user ? await user.getIdToken() : null;
  if (!token) throw new Error("Please sign in again to manage tasks.");
  const response = await fetch(`${apiUrl}${path}`, { ...init, headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}`, ...init.headers } });
  if (!response.ok) { const body = await response.json().catch(() => null) as { message?: string } | null; throw new Error(body?.message ?? "The task request could not be completed."); }
  return response.status === 204 ? (undefined as T) : response.json();
}
export const getTasks = async (projectId: number, search: string, priority: TaskPriority | "All", status?: TaskStatus | "All", page = 1, pageSize = 100) => {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize), ...(search ? { search } : {}), ...(priority !== "All" ? { priority } : {}), ...(status && status !== "All" ? { status } : {}) });
  const response = await request<TaskListResponse | Task[]>(`/api/projects/${projectId}/tasks?${query}`);
  return Array.isArray(response) ? response : response.items ?? [];
};
export const createTask = (projectId: number, body: object) => request<Task>(`/api/projects/${projectId}/tasks`, { method: "POST", body: JSON.stringify(body) });
export const updateTask = (projectId: number, taskId: number, body: object) => request<Task>(`/api/projects/${projectId}/tasks/${taskId}`, { method: "PUT", body: JSON.stringify(body) });
export const deleteTask = (projectId: number, taskId: number) => request<void>(`/api/projects/${projectId}/tasks/${taskId}`, { method: "DELETE" });
