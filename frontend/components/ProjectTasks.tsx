"use client";

import { FormEvent, useEffect, useState } from "react";
import { Project } from "../lib/projects";
import {
  createTask,
  deleteTask,
  getTasks,
  Task,
  TaskPriority,
  TaskStatus,
  updateTask,
} from "../lib/tasks";

const priorities: TaskPriority[] = ["Low", "Medium", "High"];
const statuses: TaskStatus[] = ["ToDo", "InProgress", "Completed"];
const statusLabels: Record<TaskStatus, string> = {
  ToDo: "To Do",
  InProgress: "In Progress",
  Completed: "Completed",
};

const priorityStyles: Record<TaskPriority, string> = {
  Low: "bg-emerald-50 text-emerald-800 ring-emerald-200",
  Medium: "bg-amber-50 text-amber-800 ring-amber-200",
  High: "bg-rose-50 text-rose-800 ring-rose-200",
};

const statusStyles: Record<TaskStatus, string> = {
  ToDo: "bg-slate-100 text-slate-800 ring-slate-200",
  InProgress: "bg-blue-50 text-blue-800 ring-blue-200",
  Completed: "bg-emerald-50 text-emerald-800 ring-emerald-200",
};

const controlClass =
  "w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm text-slate-900 shadow-sm outline-none placeholder:text-slate-500 focus:border-blue-600 focus:ring-2 focus:ring-blue-200";

const utcToday = () => new Date().toISOString().slice(0, 10);

export function ProjectTasks({ project }: { project: Project }) {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [search, setSearch] = useState("");
  const [priority, setPriority] = useState<TaskPriority | "All">("All");
  const [selected, setSelected] = useState<Task | null>(null);
  const [editing, setEditing] = useState<Task | null>(null);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [assignedUserId, setAssignedUserId] = useState(project.members[0]?.id ?? project.owner.id);
  const [taskPriority, setTaskPriority] = useState<TaskPriority>("Medium");
  const [status, setStatus] = useState<TaskStatus>("ToDo");
  const [dueDate, setDueDate] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    void getTasks(project.id, search, priority)
      .then(items => {
        if (active) {
          setTasks(items);
          setLoading(false);
        }
      })
      .catch(reason => {
        if (active) {
          setError(reason instanceof Error ? reason.message : "Could not load tasks.");
          setLoading(false);
        }
      });
    return () => {
      active = false;
    };
  }, [project.id, search, priority]);

  function openForm(task?: Task) {
    const item = task ?? null;
    setEditing(item);
    setTitle(item?.title ?? "");
    setDescription(item?.description ?? "");
    setAssignedUserId(item?.assignedUser.id ?? project.members[0]?.id ?? project.owner.id);
    setTaskPriority(item?.priority ?? "Medium");
    setStatus(item?.status ?? "ToDo");
    setDueDate(item?.dueDateUtc ? item.dueDateUtc.slice(0, 10) : "");
    setMessage("");
    setError("");
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!title.trim()) {
      setError("Task title is required.");
      return;
    }
    if (dueDate && dueDate < utcToday()) {
      setError("Due date cannot be in the past.");
      return;
    }

    setSaving(true);
    setError("");
    setMessage("");
    const body = {
      title: title.trim(),
      description: description.trim(),
      assignedUserId,
      priority: taskPriority,
      status,
      dueDateUtc: dueDate ? `${dueDate}T23:59:59.000Z` : null,
    };

    try {
      if (editing) {
        const item = await updateTask(project.id, editing.id, body);
        setTasks(items => items.map(task => (task.id === item.id ? item : task)));
        setSelected(item);
        setMessage("Task updated successfully.");
      } else {
        const item = await createTask(project.id, body);
        setTasks(items => [item, ...items]);
        setSelected(item);
        setMessage("Task created successfully.");
      }
      openForm();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Could not save task.");
    } finally {
      setSaving(false);
    }
  }

  async function remove(task: Task) {
    if (!window.confirm(`Delete “${task.title}”?`)) return;
    try {
      await deleteTask(project.id, task.id);
      setTasks(items => items.filter(item => item.id !== task.id));
      if (selected?.id === task.id) setSelected(null);
      setMessage("Task deleted successfully.");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Could not delete task.");
    }
  }

  const people = [project.owner, ...project.members.filter(member => member.id !== project.owner.id)];

  return (
    <section className="mt-8 rounded-2xl border border-slate-200 bg-white p-5 text-slate-900 shadow-sm sm:p-7">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.18em] text-blue-700">Task management</p>
          <h2 className="mt-1 text-2xl font-bold tracking-tight text-slate-950">Project tasks</h2>
          <p className="mt-1 text-sm text-slate-600">Plan, assign, and track work for this project.</p>
        </div>
        <button
          type="button"
          onClick={() => openForm()}
          className="rounded-lg bg-blue-700 px-4 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-blue-300 focus:ring-offset-2"
        >
          New task
        </button>
      </div>

      {message && <p role="status" className="mt-5 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800">{message}</p>}
      {error && <p role="alert" className="mt-5 rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800">{error}</p>}

      <div className="mt-6 rounded-xl border border-slate-200 bg-slate-50 p-4">
        <div className="grid gap-4 sm:grid-cols-[1fr_200px]">
          <label className="text-sm font-bold text-slate-800">
            Search tasks
            <input aria-label="Search tasks" placeholder="Search by title" value={search} onChange={event => setSearch(event.target.value)} className={`mt-2 ${controlClass}`} />
          </label>
          <label className="text-sm font-bold text-slate-800">
            Priority
            <select aria-label="Filter priority" value={priority} onChange={event => setPriority(event.target.value as TaskPriority | "All")} className={`mt-2 ${controlClass}`}>
              <option value="All">All priorities</option>
              {priorities.map(item => <option key={item} value={item}>{item}</option>)}
            </select>
          </label>
        </div>
      </div>

      {loading ? <p className="mt-6 text-sm font-medium text-slate-600">Loading tasks…</p> : tasks.length === 0 ? <p className="mt-6 rounded-xl border border-dashed border-slate-300 bg-slate-50 p-8 text-center text-sm font-medium text-slate-600">No tasks match this project and filter.</p> : <div className="mt-6 grid gap-3">{tasks.map(task => <article key={task.id} onClick={() => setSelected(task)} className={`cursor-pointer rounded-xl border p-4 transition focus-within:ring-2 focus-within:ring-blue-200 hover:border-blue-300 hover:shadow-sm ${selected?.id === task.id ? "border-blue-500 bg-blue-50/40 ring-1 ring-blue-200" : "border-slate-200 bg-white"}`}><div className="flex flex-wrap items-start justify-between gap-3"><h3 className="font-bold text-slate-950">{task.title}</h3><span className={`rounded-full px-2.5 py-1 text-xs font-bold ring-1 ${priorityStyles[task.priority]}`}>{task.priority} priority</span></div><div className="mt-3 flex flex-wrap items-center gap-2 text-sm text-slate-700"><span>{task.assignedUser.displayName}</span><span aria-hidden="true" className="text-slate-400">•</span><span className={`rounded-full px-2.5 py-1 text-xs font-bold ring-1 ${statusStyles[task.status]}`}>{statusLabels[task.status]}</span>{task.dueDateUtc && <><span aria-hidden="true" className="text-slate-400">•</span><span>Due {new Date(task.dueDateUtc).toLocaleDateString()}</span></>}</div><div className="mt-4 flex gap-4 text-sm"><button type="button" onClick={event => { event.stopPropagation(); openForm(task); }} className="font-bold text-blue-700 underline-offset-2 hover:text-blue-900 hover:underline focus:outline-none focus:ring-2 focus:ring-blue-300">Edit</button><button type="button" onClick={event => { event.stopPropagation(); void remove(task); }} className="font-bold text-rose-700 underline-offset-2 hover:text-rose-900 hover:underline focus:outline-none focus:ring-2 focus:ring-rose-300">Delete</button></div></article>)}</div>}

      {selected && <div className="mt-6 rounded-xl border border-blue-200 bg-blue-50 p-5"><p className="text-xs font-bold uppercase tracking-wider text-blue-800">Task details</p><h3 className="mt-1 text-lg font-bold text-slate-950">{selected.title}</h3><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">{selected.description || "No description provided."}</p></div>}

      <form onSubmit={submit} className="mt-7 grid gap-4 border-t border-slate-200 pt-7">
        <div><h3 className="text-lg font-bold text-slate-950">{editing ? "Edit task" : "Create task"}</h3><p className="mt-1 text-sm text-slate-600">Required fields are marked by the browser.</p></div>
        <label className="text-sm font-bold text-slate-800">Title<input required maxLength={150} placeholder="e.g. Prepare project brief" value={title} onChange={event => setTitle(event.target.value)} className={`mt-2 ${controlClass}`} /></label>
        <label className="text-sm font-bold text-slate-800">Description<textarea maxLength={2000} placeholder="Add useful context for the assignee" value={description} onChange={event => setDescription(event.target.value)} rows={3} className={`mt-2 ${controlClass}`} /></label>
        <label className="text-sm font-bold text-slate-800">Assigned member<select value={assignedUserId} onChange={event => setAssignedUserId(Number(event.target.value))} className={`mt-2 ${controlClass}`}>{people.map(person => <option key={person.id} value={person.id}>{person.displayName} ({person.email})</option>)}</select></label>
        <div className="grid gap-4 sm:grid-cols-3"><label className="text-sm font-bold text-slate-800">Priority<select value={taskPriority} onChange={event => setTaskPriority(event.target.value as TaskPriority)} className={`mt-2 ${controlClass}`}>{priorities.map(item => <option key={item} value={item}>{item}</option>)}</select></label><label className="text-sm font-bold text-slate-800">Status<select value={status} onChange={event => setStatus(event.target.value as TaskStatus)} className={`mt-2 ${controlClass}`}>{statuses.map(item => <option key={item} value={item}>{statusLabels[item]}</option>)}</select></label><label className="text-sm font-bold text-slate-800">Due date<input type="date" min={utcToday()} value={dueDate} onChange={event => setDueDate(event.target.value)} className={`mt-2 ${controlClass}`} /></label></div>
        <button disabled={saving} className="rounded-lg bg-slate-900 px-4 py-3 text-sm font-bold text-white shadow-sm transition hover:bg-slate-800 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2 disabled:cursor-not-allowed disabled:bg-slate-400">{saving ? "Saving…" : editing ? "Save task changes" : "Create task"}</button>
      </form>
    </section>
  );
}
