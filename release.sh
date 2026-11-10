#!/usr/bin/env bash
# Tags and pushes a release of the mod.
#
# The version is read from <Version> in LoreMobsSpawnReduce.csproj, the single place to change it.
# The script copies it into modinfo.json (committing that change if needed), creates tag v<version>
# and pushes master and the tag. Pushing the tag triggers .github/workflows/release.yml,
# which builds the mod and publishes the GitHub release.
#
# Usage: ./release.sh [-y|--yes]    (-y skips the confirmation prompt)
set -euo pipefail

CSPROJ=LoreMobsSpawnReduce.csproj
MODINFO=modinfo.json
BRANCH=master
REMOTE=origin

assume_yes=false
case "${1:-}" in
  -y|--yes) assume_yes=true ;;
  "") ;;
  *) echo "Usage: $0 [-y|--yes]" >&2; exit 2 ;;
esac

die() { echo "Error: $*" >&2; exit 1; }

cd "$(dirname "$0")"

[[ -z "$(git status --porcelain)" ]] || die "working tree has uncommitted changes, commit or stash them first"

echo "Updating $BRANCH..."
git checkout -q "$BRANCH"
git pull -q --ff-only "$REMOTE" "$BRANCH"

version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$CSPROJ" | head -n1)
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] \
  || die "could not read a valid version from <Version> in $CSPROJ (got '$version')"
tag="v$version"

if git rev-parse -q --verify "refs/tags/$tag" >/dev/null \
  || git ls-remote --exit-code --tags "$REMOTE" "refs/tags/$tag" >/dev/null; then
  die "tag $tag already exists, bump <Version> in $CSPROJ first"
fi

if ! grep -qxF "### $version" README.md; then
  echo "Warning: README.md has no '### $version' changelog section, so the release notes will only point to the commit history." >&2
fi

# Copy the version into modinfo.json (the only "version" key in the file).
sed -i -E "s/(\"version\"[[:space:]]*:[[:space:]]*\")[^\"]*\"/\1$version\"/" "$MODINFO"
grep -q "\"version\": *\"$version\"" "$MODINFO" || die "failed to update the version in $MODINFO"

# Make sure it builds before anything is tagged or pushed.
echo "Building $version..."
if ! dotnet build -c Release -nologo -v quiet >/dev/null; then
  git checkout -q -- "$MODINFO"
  die "build failed, run 'dotnet build -c Release' to see the errors"
fi

modinfo_changed=false
git diff --quiet -- "$MODINFO" || modinfo_changed=true

echo
echo "Release:  $tag"
if $modinfo_changed; then
  echo "Commit:   'Release $version' on $BRANCH (updates $MODINFO)"
else
  echo "Commit:   none, $MODINFO is already at $version"
fi
echo "Push:     $BRANCH and $tag to $REMOTE, which starts the GitHub release workflow"
echo

if ! $assume_yes; then
  read -r -p "Continue? [y/N] " answer
  if [[ ! "$answer" =~ ^[Yy]$ ]]; then
    git checkout -q -- "$MODINFO"
    echo "Aborted, nothing was committed or pushed."
    exit 1
  fi
fi

if $modinfo_changed; then
  git add "$MODINFO"
  git commit -q -m "Release $version"
fi

git tag -a "$tag" -m "Lore Mobs Spawn Reduce $version"
git push --atomic "$REMOTE" "$BRANCH" "$tag"

echo
echo "Pushed $tag. The release is being built by GitHub Actions:"
if repo_url=$(gh repo view --json url -q .url 2>/dev/null) && [[ -n "$repo_url" ]]; then
  echo "  $repo_url/actions"
  echo "Follow it with: gh run watch"
else
  echo "  see the repository's Actions tab on GitHub"
fi
