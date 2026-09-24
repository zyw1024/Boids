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
    static string Prefix => SessionState.GetBool("Boids.Redon.CheckAtelier",false)?"Atelier":"Redon";
    static string CheckScene => Prefix=="Atelier"?AtelierSceneBuilder.ScenePath:RedonSceneBuilder.ScenePath;
    static string ReportPath => "Captures/"+Prefix+"_RuntimeValidation.json";
    static string CapturePath(string suffix) => "Captures/"+Prefix+"_"+suffix+".png";
    static void ValidateScene(){if(Prefix=="Atelier")AtelierSceneBuilder.Validate();else RedonSceneBuilder.Validate();}
    static JObject start;
    static string startHash;
    static double nextSample;
    static bool previousBackground;
    static int phase;
    static float feedingStarted,closestBefore,closestMinimum,completedAt;
    static bool foodPlaced, outsideRejected, feedingCaptured;
    static readonly Vector3 FoodPoint = new Vector3(0,5.6f,2.4f);

    static RedonPlayCheck()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
    }

    [MenuItem("Boids/Redon/Check Reload and Play Mode")]
    public static void Run()
    {
        RunScene(false);
    }

    [MenuItem("Boids/Atelier/Check Reload and Play Mode")]
    public static void RunAtelier(){RunScene(true);}

    static void RunScene(bool atelier)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start this check in Edit mode.");
        var scene = SceneManager.GetActiveScene();
        string requestedScene=atelier?AtelierSceneBuilder.ScenePath:RedonSceneBuilder.ScenePath;
        if (scene.path != requestedScene || scene.isDirty)
            throw new InvalidOperationException("Open and save "+requestedScene+" before running the check.");
        SessionState.SetBool("Boids.Redon.CheckAtelier",atelier);

        ValidateScene();
        RedonSceneBuilder.CaptureCamera(Camera.main,CapturePath("Style"));
        string savedHash = Hash(CapturePath("Style"));
        EditorSceneManager.OpenScene(CheckScene);
        ValidateScene();
        RedonSceneBuilder.CaptureCamera(Camera.main, CapturePath("Reload"));
        SessionState.SetString(BaselineKey, new JObject
        {
            ["savedFrameSha256"] = savedHash,
            ["reloadedFrameSha256"] = Hash(CapturePath("Reload")),
            ["reloadPixels"] = ComparePixels(CapturePath("Style"), CapturePath("Reload"))
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
            phase=0;feedingCaptured=false;
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
            var school=UnityEngine.Object.FindObjectOfType<DreamSchoolController>();
            if(school==null || school.AgentCount==0 || Time.frameCount<8)return;
            if (start == null)
            {
                start = Snapshot();
                RedonSceneBuilder.CaptureCamera(Camera.main, CapturePath("PlayStart"));
                startHash = Hash(CapturePath("PlayStart"));
                nextSample = EditorApplication.timeSinceStartup + 2;
                return;
            }
            if(phase==0)
            {
                closestBefore=ClosestFishDistance();closestMinimum=closestBefore;
                outsideRejected=!school.TryFeedScreenPoint(new Vector2(-100,-100));
                foodPlaced=school.TryFeedScreenPoint(Camera.main.WorldToScreenPoint(FoodPoint));
                feedingStarted=Time.time;phase=1;nextSample=0;
                return;
            }
            if(phase==1)
            {
                closestMinimum=Mathf.Min(closestMinimum,ClosestFishDistance());
                if(school.ConsumedPortions>0 && !feedingCaptured)
                {
                    RedonSceneBuilder.CaptureCamera(Camera.main,CapturePath("Feeding"));
                    feedingCaptured=true;
                }
                if(school.CompletedFeedings>0 && school.FoodCount==0)
                {
                    completedAt=Time.time;phase=2;
                    return;
                }
                if(Time.time-feedingStarted<24)return;
                throw new InvalidOperationException("Food was not consumed within 24 simulated seconds.");
            }
            if(Time.time-completedAt<4)return;
            JObject end = Snapshot();
            RedonSceneBuilder.CaptureCamera(Camera.main, CapturePath("PlayEnd"));
            string endHash = Hash(CapturePath("PlayEnd"));
            var baseline = JObject.Parse(SessionState.GetString(BaselineKey, "{}"));
            bool persisted = (bool)baseline["reloadPixels"]["withinTolerance"];
            bool cameraStable = JToken.DeepEquals(start["camera"], end["camera"]);
            bool framesAdvanced = (int)end["frame"] > (int)start["frame"];
            bool fishMoved=(string)start["firstFishPosition"]!=(string)end["firstFishPosition"];
            bool animationsRunning=(int)end["activeFishAnimationComponents"]>=school.AgentCount;
            bool approached=closestMinimum<closestBefore*.85f;
            float dispersedDistance=ClosestFishDistance();
            bool dispersed=dispersedDistance>closestMinimum+.15f;
            bool ate=school.ConsumedPortions>=school.portionsPerFeeding && school.CompletedFeedings==1 && school.FoodCount==0;
            ValidateScene();
            for(int i=0;i<school.maxFoodClusters+2;i++)school.DropFood(FoodPoint+Vector3.right*i*.15f);
            bool clusterLimit=school.FoodCount==school.maxFoodClusters;
            bool passed = persisted && cameraStable && framesAdvanced && fishMoved && animationsRunning
                && foodPlaced && outsideRejected && approached && ate && dispersed && clusterLimit;
            Write(new
            {
                passed, checkedAtUtc = DateTime.UtcNow.ToString("O"),
                scene = CheckScene, sceneReloadStable = persisted,
                cameraStable, framesAdvanced, fishMoved, animationsRunning,
                screenPointFeeding=foodPlaced, outsideViewportRejected=outsideRejected,
                approached, foodConsumed=ate, dispersed, foodClusterLimit=clusterLimit,
                closestSixBefore=closestBefore, closestSixWhileFeeding=closestMinimum,
                closestSixAfterDispersal=dispersedDistance,
                consumedPortions=school.ConsumedPortions, completedFeedings=school.CompletedFeedings,
                feedingSeconds=completedAt-feedingStarted,
                baseline, runtimeStartSha256 = startHash, runtimeEndSha256 = endHash,
                firstSample = start, lastSample = end,
                note = "Actual Play-mode movement, screen-ray feeding, consumption and dispersal. Fixed camera; runtime images are expected to change. Not a performance benchmark."
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
                + UnityEngine.Object.FindObjectsOfType<MoonveilMotion>().Count(m => m.enabled),
            firstFishPosition=UnityEngine.Object.FindObjectOfType<MoonveilMotion>().transform.position.ToString("F5")
        });
    }

    static float ClosestFishDistance()
    {
        return UnityEngine.Object.FindObjectsOfType<MoonveilMotion>()
            .Select(m=>Vector3.Distance(m.transform.position,FoodPoint)).OrderBy(d=>d).Take(6).Average();
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
