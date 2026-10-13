#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$(cd "$SCRIPT_DIR/.." && pwd)"
VERSION_FILE="$PROJECT_PATH/ProjectSettings/ProjectVersion.txt"
ARTIFACTS_DIR="$PROJECT_PATH/.artifacts"

mkdir -p "$ARTIFACTS_DIR"

if [[ ! -f "$VERSION_FILE" ]]; then
    echo "ERROR: ProjectVersion.txt not found at $VERSION_FILE" >&2
    exit 1
fi

UNITY_VERSION=$(grep 'm_EditorVersion:' "$VERSION_FILE" | awk '{print $2}')

resolve_unity_editor() {
    if [[ -n "${UNITY_EDITOR:-}" && -x "${UNITY_EDITOR}" ]]; then
        echo "$UNITY_EDITOR"
        return 0
    fi

    local candidates=(
        "/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
        "$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity"
        "/opt/unity/Editor/Unity"
        "/c/Program Files/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity.exe"
        "C:/Program Files/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity.exe",
        "D:/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity.exe",
        "/d/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity.exe"
    )

    for candidate in "${candidates[@]}"; do
        if [[ -x "$candidate" || -f "$candidate" ]]; then
            echo "$candidate"
            return 0
        fi
    done

    echo "ERROR: Unity editor executable not found for version ${UNITY_VERSION}." >&2
    echo "Set UNITY_EDITOR environment variable to the path of the Unity executable." >&2
    exit 1
}

CMD="${1:-}"
shift || true

case "$CMD" in
    setup)
        echo "Configuring Git LFS..."
        git lfs install --local
        EDITOR_BIN="$(resolve_unity_editor)"
        EDITOR_DIR="$(dirname "$EDITOR_BIN")"
        YAML_MERGE="$EDITOR_DIR/Data/Tools/UnityYAMLMerge.exe"
        if [[ -f "$YAML_MERGE" ]]; then
            git config merge.unityyamlmerge.driver "'$YAML_MERGE' merge -p %O %B %A %A"
            echo "UnityYAMLMerge configured."
        fi
        ;;
    compile)
        EDITOR_BIN="$(resolve_unity_editor)"
        LOG_PATH="$ARTIFACTS_DIR/compile.log"
        rm -f "$LOG_PATH"
        echo "[tools/unity compile] Compiling project scripts..."
        set +e
        "$EDITOR_BIN" -batchmode -nographics -quit -projectPath "$PROJECT_PATH" -buildTarget Android -executeMethod Game.Editor.Cli.Compile -logFile "$LOG_PATH"
        EXIT_CODE=$?
        set -e

        if grep -E ':\s*error\s*CS[0-9]+:|Scripts have compiler errors' "$LOG_PATH" > /dev/null 2>&1 || [[ $EXIT_CODE -ne 0 ]] || ! grep -q '\[Game\.Editor\.Cli\] Compile succeeded\.' "$LOG_PATH"; then
            echo "[tools/unity compile] FAILED: Compilation errors detected." >&2
            grep -E ':\s*error\s*CS[0-9]+:|Scripts have compiler errors' "$LOG_PATH" >&2 || true
            exit 1
        fi
        echo "[tools/unity compile] SUCCESS: 0 errors."
        ;;
    test-edit)
        EDITOR_BIN="$(resolve_unity_editor)"
        RESULTS_PATH="$ARTIFACTS_DIR/editmode.xml"
        LOG_PATH="$ARTIFACTS_DIR/test-edit.log"
        rm -f "$RESULTS_PATH" "$LOG_PATH"
        echo "[tools/unity test-edit] Running EditMode tests..."
        set +e
        "$EDITOR_BIN" -batchmode -nographics -projectPath "$PROJECT_PATH" -runTests -testPlatform EditMode -testResults "$RESULTS_PATH" -logFile "$LOG_PATH"
        EXIT_CODE=$?
        set -e

        if [[ ! -f "$RESULTS_PATH" ]]; then
            echo "[tools/unity test-edit] FAILED: No results file generated (exit: $EXIT_CODE)." >&2
            exit 1
        fi
        FAILED_COUNT=$(grep -o 'failed="[0-9]*"' "$RESULTS_PATH" | head -1 | cut -d'"' -f2 || echo 0)
        PASSED_COUNT=$(grep -o 'passed="[0-9]*"' "$RESULTS_PATH" | head -1 | cut -d'"' -f2 || echo 0)
        TOTAL_COUNT=$(grep -o 'total="[0-9]*"' "$RESULTS_PATH" | head -1 | cut -d'"' -f2 || echo 0)

        if [[ "$FAILED_COUNT" -gt 0 || $EXIT_CODE -ne 0 ]]; then
            echo "[tools/unity test-edit] FAILED: $FAILED_COUNT/$TOTAL_COUNT tests failed." >&2
            exit 1
        fi
        echo "[tools/unity test-edit] SUCCESS: $PASSED_COUNT/$TOTAL_COUNT passed."
        ;;
    test-play)
        EDITOR_BIN="$(resolve_unity_editor)"
        RESULTS_PATH="$ARTIFACTS_DIR/playmode.xml"
        LOG_PATH="$ARTIFACTS_DIR/test-play.log"
        rm -f "$RESULTS_PATH" "$LOG_PATH"
        echo "[tools/unity test-play] Running PlayMode tests..."
        set +e
        "$EDITOR_BIN" -batchmode -nographics -projectPath "$PROJECT_PATH" -runTests -testPlatform PlayMode -testResults "$RESULTS_PATH" -logFile "$LOG_PATH"
        EXIT_CODE=$?
        set -e

        if [[ ! -f "$RESULTS_PATH" ]]; then
            echo "[tools/unity test-play] FAILED: No results file generated (exit: $EXIT_CODE)." >&2
            exit 1
        fi
        FAILED_COUNT=$(grep -o 'failed="[0-9]*"' "$RESULTS_PATH" | head -1 | cut -d'"' -f2 || echo 0)
        PASSED_COUNT=$(grep -o 'passed="[0-9]*"' "$RESULTS_PATH" | head -1 | cut -d'"' -f2 || echo 0)
        TOTAL_COUNT=$(grep -o 'total="[0-9]*"' "$RESULTS_PATH" | head -1 | cut -d'"' -f2 || echo 0)

        if [[ "$FAILED_COUNT" -gt 0 || $EXIT_CODE -ne 0 ]]; then
            echo "[tools/unity test-play] FAILED: $FAILED_COUNT/$TOTAL_COUNT tests failed." >&2
            exit 1
        fi
        echo "[tools/unity test-play] SUCCESS: $PASSED_COUNT/$TOTAL_COUNT passed."
        ;;
    record-gameplay)
        EDITOR_BIN="$(resolve_unity_editor)"
        LOG_PATH="$ARTIFACTS_DIR/record-gameplay.log"
        RESULTS_PATH="$ARTIFACTS_DIR/record-gameplay.xml"
        FRAMES_DIR="$ARTIFACTS_DIR/frames_recorded"
        TARGET_MP4="$ARTIFACTS_DIR/gameplay_recorded.mp4"
        TEST_FILTER="${1:-RecordGameplayVideo}"

        echo "[tools/unity record-gameplay] Executing PlayMode test filter '$TEST_FILTER'..."
        set +e
        "$EDITOR_BIN" -batchmode -projectPath "$PROJECT_PATH" -runTests -testPlatform PlayMode -testFilter "$TEST_FILTER" -testResults "$RESULTS_PATH" -logFile "$LOG_PATH"
        EXIT_CODE=$?
        set -e

        if [[ $EXIT_CODE -ne 0 ]]; then
            echo "[tools/unity record-gameplay] FAILED: PlayMode execution failed (exit code $EXIT_CODE)." >&2
            exit $EXIT_CODE
        fi

        if command -v ffmpeg >/dev/null 2>&1 && [[ -f "$FRAMES_DIR/frame_0000.png" || -f "$FRAMES_DIR/frame_0001.png" ]]; then
            echo "[tools/unity record-gameplay] Encoding MP4 with ffmpeg..."
            ffmpeg -y -framerate 24 -i "$FRAMES_DIR/frame_%04d.png" -c:v libx264 -pix_fmt yuv420p -crf 18 "$TARGET_MP4"
            echo "[tools/unity record-gameplay] SUCCESS: Video generated at $TARGET_MP4"
        else
            echo "[tools/unity record-gameplay] PlayMode test completed. Exit code: 0."
        fi
        ;;
    *)
        echo "Usage: $0 {setup|compile|test-edit|test-play|import-content|validate|simulate|build-android|record-gameplay}" >&2
        exit 1
        ;;
esac
