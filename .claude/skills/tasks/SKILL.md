---
name: tasks
description: Plan and manage work as a series of task files in the tasks/ directory. Use for breaking down a feature or refactor into independently implementable tasks with sequencing and acceptance criteria. Supports plan and archive subcommands.
argument-hint: "plan [description] | archive [batch-name]"
---

# Tasks Skill

## User Request

$ARGUMENTS

---

## Subcommands

### `plan [description]`

Break down the described work (or the current conversation context if no description is given) into a set of independently implementable task files.

**Workflow:**

1. **Enter plan mode** using the `EnterPlanMode` tool. Explore the codebase thoroughly before writing any files — understand what exists, what needs to change, and what the dependencies are.

2. **Design the task breakdown.** Each task should:
   - Be independently implementable (one person/agent can do it without needing another task in progress)
   - Have a single clear responsibility
   - Be completable in one sitting
   - Have unambiguous acceptance criteria

3. **Write individual task files** to `tasks/task-NN-<short-name>.md` (e.g. `task-01-remove-auth.md`). Number tasks starting at `01`, zero-padded. Each file must contain:

   ```markdown
   # Task NN — Title

   ## Why
   One paragraph explaining the motivation and how it fits the larger goal.

   ## Steps
   Numbered or bulleted concrete instructions. Include file paths, code snippets,
   and CLI commands where helpful. Be specific enough that someone unfamiliar with
   the decision context can implement it correctly.

   ## Notes
   Any gotchas, related context, or things to watch out for. Omit if none.

   ## Acceptance Criteria
   A checklist of verifiable conditions that must be true when the task is done.
   At least one criterion should be a build or test check.
   ```

4. **Write `tasks/TASKS.md`** — the top-level tracker. It must contain:
   - A **goal** section (one paragraph describing the overall change)
   - A **task table** with columns: ID, Task (linked to file), Status (`Open`/`In Progress`/`Done`), Depends On
   - A **dependency graph** (ASCII) showing which tasks block which
   - An **acceptance criteria** section describing how to verify the full batch is complete

   Status values: `Open` | `In Progress` | `Done`

5. **Exit plan mode** using `ExitPlanMode` to get user approval before any files are written.

6. After approval, write the files. Do not implement the tasks — just create the task files.

**Numbering and ordering:**
- Tasks that can run in parallel get sequential numbers but have no dependency between them
- Number them in a sensible execution order even when parallel (lowest-risk deletions first, then additions)
- Dependencies flow forward only (task-05 can depend on task-03, never the reverse)

---

### `archive [batch-name]`

Archive all completed task files so `tasks/` is clean and ready for the next batch.

**Workflow:**

1. Read `tasks/TASKS.md` to confirm all tasks are done (or note which are still open).

2. If any tasks are still `Open` or `In Progress`, warn the user and ask for confirmation before proceeding.

3. Determine the archive batch name:
   - Use the name provided in `$ARGUMENTS` if given (e.g. `archive phase-2-upload-simplification`)
   - Otherwise, infer a kebab-case name from the goal in `TASKS.md`

4. Create `tasks/archive/<batch-name>/` and move all `tasks/task-*.md` files into it.

5. Rewrite `tasks/TASKS.md` to the clean state:

   ```markdown
   # Task Tracker

   ## Open Tasks

   _No open tasks._

   ## Archive

   | Batch | Description |
   |-------|-------------|
   | [batch-name](archive/batch-name/) | One-line description of what this batch accomplished |
   ```

   Preserve any existing archive entries already in the table.

6. Commit the result with a message like:
   `Archive <batch-name> tasks`

**Do not delete task files** — they move to the archive, not the trash.
