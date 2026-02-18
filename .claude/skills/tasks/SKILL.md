---
name: tasks
description: Plan and manage work as a series of task files in the tasks/ directory. Supports multiple concurrent batches of work as subfolders. Use for breaking down a feature or refactor into independently implementable tasks with sequencing and acceptance criteria. Supports plan and archive subcommands.
argument-hint: "plan <batch-name> [description] | archive <batch-name>"
---

# Tasks Skill

## User Request

$ARGUMENTS

---

## Directory Structure

```
tasks/
  TASKS.md                          # top-level index of all active batches
  <batch-name>/
    TASKS.md                        # batch-level tracker (sequencing, deps, criteria)
    task-01-<short-name>.md
    task-02-<short-name>.md
    ...
  archive/
    <completed-batch-name>/
      TASKS.md                      # preserved from when it was active
      task-01-<short-name>.md
      ...
```

---

## Subcommands

### `plan <batch-name> [description]`

Break down the described work into a set of independently implementable task files
under `tasks/<batch-name>/`.

**The batch name** should be kebab-case and descriptive (e.g. `discord-bot-foundation`,
`user-identity`, `play-embed-posting`). It becomes the subfolder name.

**Workflow:**

1. **Enter plan mode** using the `EnterPlanMode` tool. Explore the codebase thoroughly
   before writing any files — understand what exists, what needs to change, and what
   the dependencies are within this batch.

2. **Design the task breakdown.** Each task should:
   - Be independently implementable in one sitting
   - Have a single clear responsibility
   - Have unambiguous acceptance criteria
   - Not depend on tasks from a *different* batch being in progress (cross-batch
     dependencies should be documented but not blocking within a batch)

3. **Write individual task files** to `tasks/<batch-name>/task-NN-<short-name>.md`.
   Number tasks starting at `01`, zero-padded. Each file must contain:

   ```markdown
   # Task NN — Title

   ## Why
   One paragraph explaining the motivation and how it fits the larger goal.

   ## Steps
   Numbered or bulleted concrete instructions. Include file paths, code snippets,
   and CLI commands where helpful. Be specific enough that someone unfamiliar with
   the decision context can implement it correctly.

   ## Notes
   Any gotchas, related context, or things to watch out for. Omit section if none.

   ## Acceptance Criteria
   Checklist of verifiable conditions that must be true when the task is done.
   At least one criterion should be a build or test check.
   ```

4. **Write `tasks/<batch-name>/TASKS.md`** — the batch-level tracker:
   - **Goal**: one paragraph describing the overall change this batch achieves
   - **Task table**: columns — ID, Task (linked to file), Status (`Open`/`In Progress`/`Done`), Depends On
   - **Dependency graph**: ASCII diagram showing which tasks block which
   - **Acceptance criteria**: how to verify the full batch is complete end-to-end

5. **Update `tasks/TASKS.md`** — the top-level index — adding the new batch:

   ```markdown
   # Task Tracker

   ## Active Batches

   | Batch | Description | Status |
   |-------|-------------|--------|
   | [batch-name](batch-name/) | One-line description | In Progress |

   ## Archive

   | Batch | Description |
   |-------|-------------|
   | [old-batch](archive/old-batch/) | What it accomplished |
   ```

   Preserve any existing active batches and archive entries already in the table.
   Status values for batches: `Planned` | `In Progress` | `Complete`

6. **Exit plan mode** using `ExitPlanMode` to get user approval before any files are written.

7. After approval, write the files. **Do not implement the tasks** — only create the task files.

**Numbering and ordering within a batch:**
- Tasks that can run in parallel get sequential numbers but no dependency between them
- Number in a sensible execution order even when parallel (deletions/cleanup first, then additions)
- Dependencies flow forward only (task-05 can depend on task-03, never the reverse)

---

### `archive <batch-name>`

Archive a completed batch so `tasks/<batch-name>/` is cleaned up and the top-level
index reflects its completion.

**Workflow:**

1. Read `tasks/<batch-name>/TASKS.md` to confirm all tasks are `Done`.
   If any are still `Open` or `In Progress`, warn and ask for confirmation.

2. Move `tasks/<batch-name>/` to `tasks/archive/<batch-name>/` (entire folder).

3. Update `tasks/TASKS.md`:
   - Remove the batch from the **Active Batches** table (or mark it `Complete`)
   - Add it to the **Archive** table with a one-line description of what it accomplished

**Do not delete task files** — they move to the archive, not the trash.
The archive and any implementation changes are committed together, not as a separate commit.
