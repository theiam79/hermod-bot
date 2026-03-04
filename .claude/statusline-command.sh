#!/bin/bash

# Read JSON input from stdin
input=$(cat)

# Extract data from JSON
cwd=$(echo "$input" | jq -r '.workspace.current_dir')
model=$(echo "$input" | jq -r '.model.display_name')
used_pct=$(echo "$input" | jq -r '.context_window.used_percentage // empty')

# Get short directory name
dir_name=$(basename "$cwd")

# Get git branch (skip optional locks for reliability)
git_branch=""
if git -C "$cwd" rev-parse --git-dir > /dev/null 2>&1; then
    git_branch=$(git -C "$cwd" -c core.fileMode=false branch --show-current 2>/dev/null || echo "")
    if [ -n "$git_branch" ]; then
        git_branch=" ($git_branch)"
    fi
fi

# Format context usage
context_info=""
if [ -n "$used_pct" ]; then
    context_info=" | Context: ${used_pct}%"
fi

# Output status line
printf "%s%s | %s%s" "$dir_name" "$git_branch" "$model" "$context_info"
