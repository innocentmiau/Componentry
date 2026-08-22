#!/usr/bin/env bash
#
# Builds the Asset Store layout of Componentry onto a generated orphan branch.
#
# The package lives in this repository in the layout UPM wants: the package root is the
# repository root, and it is installed under Packages/. The Asset Store delivers a plain
# folder that lands under Assets/ instead, so the files have to be rearranged before they
# can be uploaded. Rather than keep a second copy of everything by hand, this script
# generates that arrangement from whatever is committed on the current branch and writes
# it to a branch of its own.
#
# The branch is an orphan: it shares no history with main, because it holds the same
# package in a different shape rather than a continuation of the work. Each build adds a
# commit on top of the previous store build, so what was shipped when stays readable.
#
# Nothing here touches the working tree or the index. The tree is assembled in a temporary
# directory and committed through a temporary index, so the branch you are on when you run
# this is the branch you are on when it finishes.
#
# Usage:
#   Build~/build-store.sh [--branch <name>] [--out <dir>] [--allow-dirty]
#
#   --branch <name>   Branch to write. Default: store
#   --out <dir>       Also copy the built tree here, for importing into a Unity project.
#   --allow-dirty     Build even with uncommitted changes. Off by default, because the
#                     build is meant to correspond to a commit you can point at.

set -euo pipefail

BRANCH="store"
OUT_DIR=""
ALLOW_DIRTY=0

while [ $# -gt 0 ]; do
    case "$1" in
        --branch) BRANCH="${2:?--branch needs a name}"; shift 2 ;;
        --out) OUT_DIR="${2:?--out needs a directory}"; shift 2 ;;
        --allow-dirty) ALLOW_DIRTY=1; shift ;;
        -h|--help) sed -n '3,26p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *) echo "build-store: unknown option '$1'" >&2; exit 2 ;;
    esac
done

REPO="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
cd "$REPO"

# Where the package sits once the buyer imports it. Everything below is relative to this.
INSTALL_PATH="Assets/Andreleandrodev/Componentry"

# Documentation~ and Editor/Icons/Source~ are not shipped, so anything pointing into them
# has to point at the repository instead of at a folder that will not be there.
REPO_URL="https://github.com/innocentmiau/Componentry"
BLOB_URL="$REPO_URL/blob/main"

if [ "$ALLOW_DIRTY" -eq 0 ] && [ -n "$(git status --porcelain)" ]; then
    echo "build-store: working tree has uncommitted changes." >&2
    echo "             Commit them, or pass --allow-dirty to build anyway." >&2
    exit 1
fi

SOURCE_COMMIT="$(git rev-parse --short HEAD)"
SOURCE_BRANCH="$(git rev-parse --abbrev-ref HEAD)"
VERSION="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' package.json | head -1)"

if [ -z "$VERSION" ]; then
    echo "build-store: could not read version from package.json." >&2
    exit 1
fi

BUILD="$(mktemp -d)"
INDEX="$(mktemp -u)"
trap 'rm -rf "$BUILD" "$INDEX"' EXIT

PACKAGE="$BUILD/$INSTALL_PATH"
mkdir -p "$PACKAGE"

# The code and its icons, carried over with their .meta files so that asset GUIDs survive.
# A buyer upgrading keeps every reference they had, and Unity does not reimport the world.
# Source~ is left behind: the SVGs are what the PNGs were drawn from, they are never loaded
# at runtime, and the repository is where they are kept.
cp -R Editor "$PACKAGE/Editor"
cp Editor.meta "$PACKAGE/Editor.meta"
rm -rf "$PACKAGE/Editor/Icons/Source~"

# package.json describes a UPM package, and this is not being installed as one. Shipping it
# would say the folder can be moved to Packages/, which is not true of a copy under Assets/.
# The version a buyer has is in CHANGELOG.md, at the top, where they would look for it.
for doc in README.md CHANGELOG.md LICENSE.md "Third Party Notices.md"; do
    cp "$doc" "$PACKAGE/$doc"
    if [ -f "$doc.meta" ]; then
        cp "$doc.meta" "$PACKAGE/$doc.meta"
    fi
done

# README links into Documentation~ four times, for three screenshots and for the full guide.
# Those resolve on GitHub and nowhere else, so in a shipped copy they become absolute.
# The links to Editor/... are left as they are: those folders do ship, right beside the README.
sed -i.bak "s#](Documentation~/#]($BLOB_URL/Documentation~/#g" "$PACKAGE/README.md"
sed -i.bak "s#\[Documentation~/Componentry.md\]#[the full documentation]#g" "$PACKAGE/README.md"

# Same problem in the notices: it names a folder that is not in this copy.
sed -i.bak "s#\`Editor/Icons/Source~\`#the repository, under \`Editor/Icons/Source~\` ($BLOB_URL/Editor/Icons/Source~)#g" "$PACKAGE/Third Party Notices.md"

rm -f "$PACKAGE"/*.bak

if grep -rn "](Documentation~/" "$PACKAGE" >/dev/null 2>&1; then
    echo "build-store: a relative Documentation~ link survived the rewrite." >&2
    exit 1
fi

if [ -e "$PACKAGE/Editor/Icons/Source~" ] || [ -e "$PACKAGE/package.json" ]; then
    echo "build-store: something that should not ship is in the build." >&2
    exit 1
fi

# An asset that arrives without its .meta is given a new GUID on import, which breaks every
# reference a buyer already had to it. Cheap to check and expensive to notice later.
MISSING_META=0
while IFS= read -r asset; do
    case "$asset" in *.meta) continue ;; esac
    if [ ! -e "$asset.meta" ]; then
        echo "build-store: no .meta for ${asset#$PACKAGE/}" >&2
        MISSING_META=1
    fi
done < <(find "$PACKAGE" -mindepth 1)

if [ "$MISSING_META" -ne 0 ]; then
    exit 1
fi

# Committed through an index of its own, so the repository's real index is never written to
# and the checkout is never switched. git add is forced because a global ignore file would
# otherwise be free to drop files out of the package without saying so.
export GIT_INDEX_FILE="$INDEX"
TREE="$(cd "$BUILD" && git --git-dir="$REPO/.git" --work-tree="$BUILD" add --force --all && git --git-dir="$REPO/.git" --work-tree="$BUILD" write-tree)"
unset GIT_INDEX_FILE

PROVENANCE="$SOURCE_BRANCH at $SOURCE_COMMIT"
if [ -n "$(git status --porcelain)" ]; then
    PROVENANCE="$PROVENANCE, plus uncommitted working tree changes"
fi

MESSAGE="Asset Store layout $VERSION

Generated by Build~/build-store.sh from $PROVENANCE.
Do not commit here by hand: the next build replaces this tree."

if PARENT="$(git rev-parse --verify --quiet "refs/heads/$BRANCH")"; then
    COMMIT="$(git commit-tree "$TREE" -p "$PARENT" -m "$MESSAGE")"
else
    COMMIT="$(git commit-tree "$TREE" -m "$MESSAGE")"
fi

git update-ref "refs/heads/$BRANCH" "$COMMIT"

if [ -n "$OUT_DIR" ]; then
    # Copied as contents rather than as the folder, so that pointing --out at a Unity project
    # root merges into the Assets folder already there instead of nesting an Assets inside it.
    mkdir -p "$OUT_DIR/Assets"
    cp -R "$BUILD/Assets/." "$OUT_DIR/Assets/"
fi

FILES="$(git ls-tree -r --name-only "$COMMIT" | wc -l | tr -d ' ')"

echo "Built $VERSION onto '$BRANCH' as ${COMMIT:0:7} from $SOURCE_BRANCH at $SOURCE_COMMIT."
echo "$FILES files under $INSTALL_PATH."
echo
echo "Inspect it:   git show --stat $BRANCH"
echo "              git ls-tree -r --name-only $BRANCH"
[ -n "$OUT_DIR" ] && echo "Copied to:    $OUT_DIR/Assets"
echo
echo "To upload: copy the Assets/ tree into a Unity 2022.3+ project, then point"
echo "Asset Store Tools at $INSTALL_PATH."
echo "Nothing has been pushed. To publish the branch:  git push -f origin $BRANCH"
