using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Boids.Art;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Checks scene persistence and an actual Play-mode interval, independently of MCP reconnects.</summary>
[InitializeOnLoad]
public static class RedonPlayCheck
{
    const string RunningKey = "Boids.Redon.CheckRunning";
    const string BaselineKey = "Boids.Redon.CheckBaseline";
    const string ReportPath = "Captures/Redon_RuntimeValidation.json";
    static JObject start;
    static string startHash;
    static double nextSample;
    static bool previousBackground;

    static RedonPlayCheck()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
    }

    [MenuItem("Boids/Redon/Check Reload and Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start this check in Edit mode.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != RedonSceneBuilder.ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open and save RedonDream before running the check.");

        RedonSceneBuilder.Validate();
        RedonSceneBuilder.Capture();
        string savedHash = Hash("Captures/Redon_Style.png");
        EditorSceneManager.OpenScene(RedonSceneBuilder.ScenePath);
        RedonSceneBuilder.Validate();
        RedonSceneBuilder.CaptureCamera(Camera.main, "Captures/Redon_Reload.png");
        SessionState.SetString(BaselineKey, new JObject
        {
            ["savedFrameSha256"] = savedHash,
            ["reloadedFrameSha256"] = Hash("Captures/Redon_Reload.png"),
            ["reloadPixels"] = ComparePixels("Captures/Redon_Style.png", "Captures/Redon_Reload.png")
        }.ToString());
        SessionState.SetBool(RunningKey, true);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            start = null;
            nextSample = EditorApplication.timeSinceStartup + 1;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Write(new { passed = false, reason = "Play mode ended before both samples were captured." });
            Cleanup();
        }
    }

    static void Tick()
    {
        if (!Application.isPlaying || EditorApplication.timeSinceStartup < nextSample) return;
        try
        {
            if (start == null)
            {
                start = Snapshot();
                RedonSceneBuilder.CaptureCamera(Camera.main, "Captures/Redon_PlayStart.png");
                startHash = Hash("Captures/Redon_PlayStart.png");
                nextSample = EditorApplication.timeSinceStartup + 3;
                return;
            }
            JObject end = Snapshot();
            RedonSceneBuilder.CaptureCamera(Camera.main, "Captures/Redon_PlayEnd.png");
            string endHash = Hash("Captures/Redon_PlayEnd.png");
            var baseline = JObject.Parse(SessionState.GetString(BaselineKey, "{}"));
            bool persisted = (bool)baseline["reloadPixels"]["withinTolerance"];
            var runtimePixels = ComparePixels("Captures/Redon_PlayStart.png", "Captures/Redon_PlayEnd.png");
            var editorPixels = ComparePixels("Captures/Redon_Style.png", "Captures/Redon_PlayEnd.png");
            bool cameraStable = JToken.DeepEquals(start["camera"], end["camera"]);
            bool framesAdvanced = (int)end["frame"] > (int)start["frame"];
            bool staticFish = (int)start["activeFishAnimationComponents"] == 0
                && (int)end["activeFishAnimationComponents"] == 0;
            RedonSceneBuilder.Validate();
            bool passed = persisted && cameraStable && framesAdvanced && staticFish
                && (bool)runtimePixels["withinTolerance"] && (bool)editorPixels["withinTolerance"];
            Write(new
            {
                passed, checkedAtUtc = DateTime.UtcNow.ToString("O"),
                scene = RedonSceneBuilder.ScenePath, sceneReloadStable = persisted,
                cameraStable, framesAdvanced, fishAnimationsDisabled = staticFish,
                runtimeFramesIdentical = startHash == endHash,
                runtimeFramesStable = (bool)runtimePixels["withinTolerance"],
                runtimeMatchesEditorWithinTolerance = (bool)editorPixels["withinTolerance"],
                runtimePixels, editorPixels,
                baseline, runtimeStartSha256 = startHash, runtimeEndSha256 = endHash,
                firstSample = start, lastSample = end,
                note = "Editor Play-mode stability check, not a performance benchmark or standalone build."
            });
            Cleanup();
            EditorApplication.isPlaying = false;
            Debug.Log(passed ? "Redon reload and Play-mode check passed." : "Redon Play-mode check failed; inspect the report.");
        }
        catch (Exception error)
        {
            Write(new { passed = false, reason = error.Message });
            Cleanup();
            EditorApplication.isPlaying = false;
            Debug.LogException(error);
        }
    }

    static JObject Snapshot()
    {
        var c = Camera.main;
        if (!Application.isPlaying || c == null) throw new InvalidOperationException("No running scene camera.");
        return JObject.FromObject(new
        {
            playing = Application.isPlaying, frame = Time.frameCount, seconds = Time.time,
            camera = new
            {
                position = c.transform.position.ToString("F5"), rotation = c.transform.rotation.ToString("F5"),
                orthographic = c.orthographic, size = c.orthographicSize, aspect = c.aspect
            },
            activeFishAnimationComponents = UnityEngine.Object.FindObjectsOfType<Animator>().Count(a => a.enabled)
                + UnityEngine.Object.FindObjectsOfType<MoonveilMotion>().Count(m => m.enabled)
        });
    }

    static void Cleanup()
    {
        SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= Tick;
        Application.runInBackground = previousBackground;
    }
    static void Write(object report) => File.WriteAllText(ReportPath, JsonConvert.SerializeObject(report, Formatting.Indented));
    static JObject ComparePixels(string first, string second)
    {
        var a = new Texture2D(2, 2);
        var b = new Texture2D(2, 2);
        try
        {
            a.LoadImage(File.ReadAllBytes(first));
            b.LoadImage(File.ReadAllBytes(second));
            if (a.width != b.width || a.height != b.height)
                throw new InvalidOperationException("Capture dimensions changed.");
            var ac = a.GetPixels32();
            var bc = b.GetPixels32();
            int changed = 0, largest = 0;
            long total = 0;
            for (int i = 0; i < ac.Length; i++)
            {
                int r = Math.Abs(ac[i].r - bc[i].r);
                int g = Math.Abs(ac[i].g - bc[i].g);
                int bl = Math.Abs(ac[i].b - bc[i].b);
                if (r + g + bl > 0) changed++;
                total += r + g + bl;
                largest = Math.Max(largest, Math.Max(r, Math.Max(g, bl)));
            }
            // Allow tiny isolated 8-bit render differences; retain exact hashes and all measurements.
            return JObject.FromObject(new
            {
                pixelCount = ac.Length, differentPixels = changed, maxChannelDelta = largest,
                meanChannelDelta = (double)total / (ac.Length * 3),
                allowedMaxChannelDelta = 2, allowedDifferentPixelFraction = 0.0001,
                withinTolerance = largest <= 2 && (double)changed / ac.Length <= 0.0001
            });
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
        }
    }
    static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
}
