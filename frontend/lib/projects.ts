import { auth } from "./firebase";

export type ProjectUser = { id: number; firebaseUid: string; displayName: string; email: string };
export type Project = {
  id: number;
  name: string;
  description: string;
  owner: ProjectUser;
  members: ProjectUser[];
  createdAtUtc: string;
  updatedAtUtc: string;
};

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5159";

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const user = auth.currentUser;
  const token = user ? await user.getIdToken() : null;
  if (!token) throw new Error("Please sign in again to manage projects.");
  const response = await fetch(`${apiUrl}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}`, ...init.headers },
  });
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(body?.message ?? "The project request could not be completed.");
  }
  return response.status === 204 ? (undefined as T) : response.json();
}

export const getProjects = () => request<Project[]>("/api/projects");
export const getProject = (id: string) => request<Project>(`/api/projects/${id}`);
export const createProject = (name: string, description: string, memberEmails: string[]) => request<Project>("/api/projects", { method: "POST", body: JSON.stringify({ name, description, memberEmails }) });
export const updateProject = (id: string, name: string, description: string, memberEmails: string[]) => request<Project>(`/api/projects/${id}`, { method: "PUT", body: JSON.stringify({ name, description, memberEmails }) });
export const deleteProject = (id: string) => request<void>(`/api/projects/${id}`, { method: "DELETE" });
