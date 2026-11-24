#!/usr/bin/env bash
# Repository hygiene checks for a Unity project. Runs without a Unity licence.
#  1. Unity-generated folders must never be committed.
#  2. Every asset under Assets/ has a .meta file, and every .meta file has its asset.
#  3. No tracked file over 50 MB (large binaries must go through Git LFS).
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
fail=0

echo "== 1. Forbidden folders"
forbidden=$(git ls-files | grep -E '^(Library|Temp|Obj|Logs|UserSettings|Build|Builds)/' || true)
if [ -n "$forbidden" ]; then
  echo "ERROR: generated folders are tracked:"; echo "$forbidden" | head -20; fail=1
else echo "ok"; fi

echo "== 2. Meta files"
tracked=$(git ls-files 'Assets/*')
# Every tracked asset file needs a tracked .meta
while IFS= read -r f; do
  [ -z "$f" ] && continue
  case "$f" in *.meta) continue ;; esac
  if ! grep -qxF "$f.meta" <<< "$tracked"; then echo "ERROR: missing meta for $f"; fail=1; fi
done <<< "$tracked"
# Every folder under Assets/ needs a .meta (Assets itself does not)
dirs=$(echo "$tracked" | xargs -d '\n' -n1 dirname | sort -u | grep -v '^Assets$' || true)
while IFS= read -r d; do
  [ -z "$d" ] && continue
  if ! grep -qxF "$d.meta" <<< "$tracked"; then echo "ERROR: missing folder meta for $d"; fail=1; fi
done <<< "$dirs"
# Every .meta needs its asset (file or folder)
while IFS= read -r m; do
  [ -z "$m" ] && continue
  case "$m" in *.meta) ;; *) continue ;; esac
  base="${m%.meta}"
  if ! grep -qxF "$base" <<< "$tracked" && ! grep -q "^$base/" <<< "$tracked"; then
    echo "ERROR: orphan meta $m"; fail=1
  fi
done <<< "$tracked"
[ $fail -eq 0 ] && echo "ok"

echo "== 3. Large files"
large=0
while IFS= read -r f; do
  [ -f "$f" ] || continue
  size=$(wc -c < "$f")
  if [ "$size" -gt 52428800 ]; then echo "ERROR: $f is $((size/1048576)) MB; track it with Git LFS"; large=1; fi
done < <(git ls-files)
[ $large -eq 0 ] && echo "ok" || fail=1

if [ $fail -ne 0 ]; then echo "Repository checks FAILED"; exit 1; fi
echo "All repository checks passed"
