"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { AuthGuard } from "@/components/AuthGuard";
import { ProjectTasks } from "@/components/ProjectTasks";
import { getProject, Project } from "@/lib/projects";

function TasksPageContent() {
  const { id } = useParams<{ id: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [error, setError] = useState("");
  useEffect(() => { void getProject(id).then(setProject).catch(reason => setError(reason instanceof Error ? reason.message : "Could not load the project.")); }, [id]);
  if (error) return <main className="min-h-screen bg-slate-100 p-10"><p role="alert" className="mx-auto max-w-4xl rounded-lg bg-red-50 p-4 text-red-700">{error}</p></main>;
  if (!project) return <main className="min-h-screen bg-slate-100 p-10 text-slate-500">Loading tasks…</main>;
  return <main className="min-h-screen bg-slate-100"><header className="border-b border-slate-200 bg-white"><div className="mx-auto flex max-w-4xl items-center justify-between px-6 py-4"><Link href={`/projects/${id}`} className="font-bold text-slate-900">← {project.name}</Link><Link href="/dashboard" className="text-sm font-semibold text-blue-600">Dashboard</Link></div></header><div className="mx-auto max-w-4xl px-6 py-10"><ProjectTasks project={project} /></div></main>;
}

export default function ProjectTasksPage() { return <AuthGuard><TasksPageContent /></AuthGuard>; }
