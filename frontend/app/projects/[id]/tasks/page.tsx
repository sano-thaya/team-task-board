"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { AuthGuard } from "@/components/AuthGuard";
import { ProjectTasks } from "@/components/ProjectTasks";
import { getProject, type Project } from "@/lib/projects";

function TasksPageContent() {
  const { id } = useParams<{ id: string }>(); const [project, setProject] = useState<Project | null>(null); const [error, setError] = useState("");
  useEffect(() => { void getProject(id).then(setProject).catch(reason => setError(reason instanceof Error ? reason.message : "Could not load the project.")); }, [id]);
  if (error) return <main className="min-h-screen bg-slate-100 px-4 py-10 sm:px-6"><p role="alert" className="mx-auto max-w-4xl rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p></main>;
  if (!project) return <main className="flex min-h-screen items-center justify-center bg-slate-100 px-6 text-slate-600" aria-live="polite">Loading tasks…</main>;
  return <main className="min-h-screen bg-slate-100"><header className="border-b border-slate-200 bg-white"><div className="mx-auto flex max-w-4xl flex-wrap items-center justify-between gap-3 px-4 py-4 sm:px-6"><Link href={`/projects/${id}`} className="rounded-md px-2 py-1 font-bold text-slate-900 hover:bg-slate-100">← <span className="sr-only">Back to </span>{project.name}</Link><nav aria-label="Task navigation" className="flex gap-4"><Link href={`/projects/${id}`} className="text-sm font-semibold text-blue-700 hover:underline">Project details</Link><Link href="/dashboard" className="text-sm font-semibold text-blue-700 hover:underline">Dashboard</Link></nav></div></header><div className="mx-auto max-w-4xl px-4 py-6 sm:px-6 sm:py-10"><ProjectTasks project={project} /></div></main>;
}

export default function ProjectTasksPage() { return <AuthGuard><TasksPageContent /></AuthGuard>; }
