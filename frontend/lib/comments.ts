import { doc, onSnapshot, type Unsubscribe } from "firebase/firestore";
import { auth, db } from "./firebase";

export type TaskComment = { id: string; authorName: string; text: string; createdAtUtc: string };
const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5159";

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const user = auth.currentUser;
  const token = user ? await user.getIdToken() : null;
  if (!token) throw new Error("Please sign in again to view comments.");
  const response = await fetch(`${apiUrl}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}`, ...init.headers },
  });
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(body?.message ?? "The comments request could not be completed.");
  }
  return response.json();
}

export const getComments = (projectId: number, taskId: number) => request<TaskComment[]>(`/api/projects/${projectId}/tasks/${taskId}/comments`);
export const createComment = (projectId: number, taskId: number, text: string) => request<TaskComment>(`/api/projects/${projectId}/tasks/${taskId}/comments`, { method: "POST", body: JSON.stringify({ text }) });

export function subscribeToCommentUpdates(projectId: number, taskId: number, onChanged: () => void, onError: (error: Error) => void): Unsubscribe {
  return onSnapshot(doc(db, "commentEvents", `${projectId}_${taskId}`), () => onChanged(), error => onError(error));
}
