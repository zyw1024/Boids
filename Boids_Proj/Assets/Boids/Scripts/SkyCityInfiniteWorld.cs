using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Boids.Art.Infinite
{
    [DisallowMultipleComponent]
    public sealed class SkyCityInfiniteWorld : MonoBehaviour
    {
        public Camera view;
        public Material architectureMaterial, waterMaterial, cascadeMaterial;
        public int seed = 93641;
        [Range(1,3)] public int loadRadius = 2;
        [Range(9,49)] public int maximumResidentChunks = 25;
        [Range(1,3)] public int maximumWorkers = 2;
        [Range(.5f,5)] public float mainThreadBudgetMilliseconds = 2;
        public float rebaseThreshold = 384;
        public bool showDiagnostics;

        public bool Ready { get; private set; }
        public string Status { get; private set; } = "Opening the atlas";
        public long OriginX { get; private set; }
        public long OriginZ { get; private set; }
        public int ResidentCount { get { return chunks.Count; } }
        public int PendingCount { get { return jobs.Count + (staging != null ? 1 : 0); } }
        public int PoolCount { get { return pool.Count; } }
        public int CompletedChunks { get; private set; }
        public int UnloadedChunks { get; private set; }
        public int CancelledJobs { get; private set; }
        public int FailedJobs { get; private set; }
        public int Fallbacks { get; private set; }
        public int OriginShifts { get; private set; }
        public int InitialDistrictObjects { get; private set; }
        public int PeakResident { get; private set; }
        public int PeakPending { get; private set; }
        public double LastMainThreadMs { get; private set; }
        public double PeakMainThreadMs { get; private set; }
        public double LastWorkerMs { get; private set; }
        public long MeshBytes { get; private set; }
        public long LibraryBytes { get { return library == null ? 0 : library.Bytes; } }
        public int Uploads { get; private set; }
        public double PeakUploadMs { get; private set; }
        public double FirstIslandMilliseconds { get; private set; }
        public SkyCityModuleData Library { get { return library; } }

        sealed class Chunk
        {
            public GameObject root;
            public MeshFilter[] filters = new MeshFilter[6];
            public MeshRenderer[] renderers = new MeshRenderer[6];
            public SkyCityWfc.Result layout;
            public int lod;
            public long bytes;
            public float appeared;
            public float appliedReveal=-1;
            public Vector4 appliedBridges;
        }
        sealed class Job
        {
            public SkyCityWfc.Coord coord;
            public int lod;
            public CancellationTokenSource cancellation;
            public Task<SkyCityModuleData.Payload> task;
        }
        sealed class Staging
        {
            public Chunk chunk;
            public SkyCityModuleData.Payload payload;
            public Mesh[] fresh = new Mesh[6];
            public int cursor;
            public bool replacement;
        }
        readonly Dictionary<SkyCityWfc.Coord,Chunk> chunks = new Dictionary<SkyCityWfc.Coord,Chunk>();
        readonly List<SkyCityWfc.Coord> wanted = new List<SkyCityWfc.Coord>(49);
        readonly HashSet<SkyCityWfc.Coord> wantedSet = new HashSet<SkyCityWfc.Coord>();
        readonly List<Job> jobs = new List<Job>(3);
        readonly Stack<Chunk> pool = new Stack<Chunk>(2);
        MaterialPropertyBlock appearance;
        CancellationTokenSource lifetime;
        SkyCityModuleData library;
        Staging staging;
        float nextPlan;
        Vector3 priorityPosition, priorityForward;
        double startedAt;
        int previousTargetFrameRate,previousVsync;
        SkyCityWaterReflection reflection;
        const int PoolLimit = 2;
        void Awake() { InitialDistrictObjects = transform.childCount; }

        IEnumerator Start()
        {
            appearance = new MaterialPropertyBlock();
            lifetime = new CancellationTokenSource(); previousTargetFrameRate = Application.targetFrameRate;
            previousVsync=QualitySettings.vSyncCount;QualitySettings.vSyncCount=0;
            reflection=FindObjectOfType<SkyCityWaterReflection>();
            Application.targetFrameRate = 60; startedAt = Time.realtimeSinceStartupAsDouble;
            if (view == null) view = Camera.main;
            if (view == null || architectureMaterial == null || waterMaterial == null || cascadeMaterial == null)
            { Status = "World references are missing"; Debug.LogError(Status); yield break; }
            var request = Resources.LoadAsync<TextAsset>("SkyCityInfinite/Modules");
            yield return request;
            var asset = request.asset as TextAsset;
            if (asset == null) { Status = "Module atlas is missing"; Debug.LogError(Status); yield break; }
            byte[] bytes = asset.bytes; // The shared vocabulary; no generated world chunks.
            var cancellation = lifetime.Token;
            var decode = Task.Run(() => SkyCityModuleData.Read(bytes, cancellation), cancellation);
            Status = "Preparing the architectural vocabulary";
            while (!decode.IsCompleted) yield return null;
            Resources.UnloadAsset(asset);
            if (decode.IsFaulted) { Status = "Module atlas failed to decode"; Debug.LogException(decode.Exception); yield break; }
            if (decode.IsCanceled) yield break;
            library = decode.Result; Ready = true; nextPlan = 0;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.H)) showDiagnostics = !showDiagnostics;
            if (!Ready || view == null) return;
            long begin = Stopwatch.GetTimestamp();
            Rebase();
            if (Time.unscaledTime >= nextPlan) { Plan(); nextPlan = Time.unscaledTime + .18f; }
            ReapCancelled();
            // Each atomic operation is one patch upload. Do not start another past budget.
            if (staging != null) UploadOne();
            while (Elapsed(begin) < mainThreadBudgetMilliseconds)
            {
                if (staging != null) { UploadOne(); continue; }
                if (BeginReady()) continue;
                if (EvictOne(false)) continue;
                break;
            }
            Schedule();
            UpdateAppearance();
            PeakResident = Math.Max(PeakResident, ResidentCount);
            PeakPending = Math.Max(PeakPending, PendingCount);
            Status = CompletedChunks == 0 ? "Assembling the first islands" : "The Garden of Endless Winds";
            LastMainThreadMs = Elapsed(begin); PeakMainThreadMs = Math.Max(PeakMainThreadMs, LastMainThreadMs);
        }
        static double Elapsed(long start) { return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency; }
        public SkyCityWfc.Coord CurrentCoord()
        {
            return new SkyCityWfc.Coord(OriginX + (long)Math.Floor(view.transform.position.x / SkyCityWfc.ChunkSize),
                OriginZ + (long)Math.Floor(view.transform.position.z / SkyCityWfc.ChunkSize));
        }
        Vector3 Position(SkyCityWfc.Coord c)
        { return new Vector3((float)(c.x - OriginX) * SkyCityWfc.ChunkSize, 0, (float)(c.z - OriginZ) * SkyCityWfc.ChunkSize); }
        void Plan()
        {
            Vector3 ahead=view.transform.position+Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized*52;
            var center = new SkyCityWfc.Coord(OriginX+(long)Math.Floor(ahead.x/SkyCityWfc.ChunkSize),OriginZ+(long)Math.Floor(ahead.z/SkyCityWfc.ChunkSize));
            wanted.Clear(); wantedSet.Clear();
            int radius = Mathf.Clamp(loadRadius,1,3);
            for (int z = -radius; z <= radius; z++) for (int x = -radius; x <= radius; x++)
                wanted.Add(new SkyCityWfc.Coord(center.x + x, center.z + z));
            priorityPosition = view.transform.position; priorityForward = view.transform.forward;
            wanted.Sort(ComparePriority);
            if(reflection!=null&&wanted.Count>0)reflection.waterHeight=SkyCityWfc.Elevation(SkyCityWfc.Composition(wanted[0],seed))+.38f;
            if (wanted.Count > maximumResidentChunks) wanted.RemoveRange(maximumResidentChunks,wanted.Count-maximumResidentChunks);
            foreach (var key in wanted) wantedSet.Add(key);
            foreach (var job in jobs) if (!wantedSet.Contains(job.coord) && !job.cancellation.IsCancellationRequested)
            { job.cancellation.Cancel(); CancelledJobs++; }
            if (staging != null && !wantedSet.Contains(staging.payload.layout.coord)) AbortStaging();
        }
        int ComparePriority(SkyCityWfc.Coord a, SkyCityWfc.Coord b) { return Priority(a).CompareTo(Priority(b)); }
        float Priority(SkyCityWfc.Coord c)
        { Vector3 delta = Position(c) + new Vector3(42,18,42) - priorityPosition; return delta.sqrMagnitude - Vector3.Dot(delta,priorityForward) * 65; }
        int DesiredLod(SkyCityWfc.Coord c, int previous = -1)
        {
            Vector3 delta = Position(c) + new Vector3(42,18,42) - view.transform.position;
            float distance = new Vector2(delta.x,delta.z).magnitude;
            // Hysteresis prevents rebuilding around a detail threshold every frame.
            if (previous == 0 && distance < 185) return 0;
            if (previous == 1 && distance >= 155 && distance < 325) return 1;
            if (previous == 2 && distance >= 290) return 2;
            return distance < 170 ? 0 : distance < 308 ? 1 : 2;
        }
        void Schedule()
        {
            if (jobs.Count >= maximumWorkers) return;
            foreach (var coord in wanted)
            {
                Chunk current; chunks.TryGetValue(coord,out current);
                int lod = DesiredLod(coord,current == null ? -1 : current.lod);
                if (current != null && current.lod == lod) continue;
                bool exists = staging != null && staging.payload.layout.coord.Equals(coord);
                foreach (var job in jobs) if (job.coord.Equals(coord)) exists = true;
                if (exists) continue;
                var taskCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                CancellationToken token = taskCancellation.Token;
                var vocabulary = library; var layout = current == null ? null : current.layout; int worldSeed = seed;
                var jobItem = new Job { coord = coord, lod = lod, cancellation = taskCancellation };
                jobItem.task = Task.Run(() =>
                {
                    var clock = Stopwatch.StartNew();
                    var solved = layout ?? SkyCityWfc.Solve(coord,worldSeed,token);
                    var payload = vocabulary.Build(solved,lod,token); payload.workerMilliseconds = clock.Elapsed.TotalMilliseconds; return payload;
                },token);
                jobs.Add(jobItem);
                if (jobs.Count >= maximumWorkers) break;
            }
        }
        void ReapCancelled()
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                var j = jobs[i]; if (!j.task.IsCompleted) continue;
                if (j.task.IsFaulted) { FailedJobs++; Debug.LogException(j.task.Exception); }
                if (j.task.IsCanceled || j.task.IsFaulted || !wantedSet.Contains(j.coord))
                { j.cancellation.Dispose(); jobs.RemoveAt(i); }
            }
        }
        bool BeginReady()
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i]; if (j.task.Status != TaskStatus.RanToCompletion) continue;
                Chunk chunk; bool replacement = chunks.TryGetValue(j.coord,out chunk);
                if (!replacement && chunks.Count >= maximumResidentChunks && !EvictOne(true)) return false;
                var payload = j.task.Result; LastWorkerMs = payload.workerMilliseconds;
                j.cancellation.Dispose(); jobs.RemoveAt(i);
                if (chunk == null) chunk = BorrowChunk();
                staging = new Staging { chunk = chunk, payload = payload, replacement = replacement }; return true;
            }
            return false;
        }
        Chunk BorrowChunk()
        {
            if (pool.Count > 0) return pool.Pop();
            var c = new Chunk { root = new GameObject("Streaming island district") };
            c.root.transform.SetParent(transform,false); c.root.SetActive(false);
            for (int i = 0; i < 6; i++)
            {
                var child = new GameObject(i == 5 ? "Transparent cascades" : i == 4 ? "Reflecting pools" : "Architecture patch " + i);
                child.transform.SetParent(c.root.transform,false); if (i >= 4) child.layer = 4;
                c.filters[i] = child.AddComponent<MeshFilter>(); var renderer = child.AddComponent<MeshRenderer>(); c.renderers[i] = renderer;
                renderer.sharedMaterial = i == 5 ? cascadeMaterial : i == 4 ? waterMaterial : architectureMaterial;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes; renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
            }
            return c;
        }
        void UploadOne()
        {
            if (staging == null) return;
            var s = staging; int i = s.cursor; long begin = Stopwatch.GetTimestamp();
            var data = i == 5 ? s.payload.cascades : i == 4 ? s.payload.water : s.payload.opaque[i];
            s.fresh[i] = SkyCityModuleData.Upload(data,"District " + s.payload.layout.coord + " / LOD " + s.payload.lod + " / " + i);
            Uploads++; PeakUploadMs = Math.Max(PeakUploadMs,Elapsed(begin)); s.cursor++;
            if (s.cursor < 6) return;
            var c = s.chunk; MeshBytes -= c.bytes; c.bytes = s.payload.Bytes; MeshBytes += c.bytes;
            for (int part = 0; part < 6; part++)
            {
                if (c.filters[part].sharedMesh != null) Destroy(c.filters[part].sharedMesh);
                c.filters[part].sharedMesh = s.fresh[part];
                c.renderers[part].shadowCastingMode = part >= 4 || s.payload.lod == 2 ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }
            c.layout = s.payload.layout; c.lod = s.payload.lod; c.root.transform.position = Position(c.layout.coord);
            if (!s.replacement)
            {
                chunks.Add(c.layout.coord,c); CompletedChunks++; if (c.layout.usedSafeFallback) Fallbacks++;
                if(CompletedChunks==1)FirstIslandMilliseconds=(Time.realtimeSinceStartupAsDouble-startedAt)*1000;
                c.appeared = Time.unscaledTime;
                c.appliedReveal=-1;
            }
            c.root.SetActive(true); staging = null;
        }
        bool EvictOne(bool capacityPressure)
        {
            SkyCityWfc.Coord key = default(SkyCityWfc.Coord); bool found = false; float farthest = -1;
            var center = CurrentCoord();
            foreach (var pair in chunks)
            {
                if (wantedSet.Contains(pair.Key)) continue;
                long dx = Math.Abs(pair.Key.x-center.x), dz = Math.Abs(pair.Key.z-center.z);
                if (!capacityPressure && dx <= loadRadius + 1 && dz <= loadRadius + 1) continue;
                float score = (Position(pair.Key)-view.transform.position).sqrMagnitude;
                if (score > farthest) { farthest = score; key = pair.Key; found = true; }
            }
            if (!found) return false;
            var c = chunks[key]; chunks.Remove(key); MeshBytes -= c.bytes; ReturnChunk(c); UnloadedChunks++; return true;
        }
        void ReturnChunk(Chunk c)
        {
            c.root.SetActive(false);
            foreach (var filter in c.filters) if (filter.sharedMesh != null) { Destroy(filter.sharedMesh); filter.sharedMesh = null; }
            c.bytes = 0; c.layout = null;
            if (pool.Count < PoolLimit) pool.Push(c); else Destroy(c.root);
        }
        void AbortStaging()
        {
            foreach (var mesh in staging.fresh) if (mesh != null) Destroy(mesh);
            if (!staging.replacement) ReturnChunk(staging.chunk);
            staging = null;
        }
        void UpdateAppearance()
        {
            foreach (var pair in chunks)
            {
                var c = pair.Value;
                float alpha = Mathf.Clamp01((Time.unscaledTime-c.appeared)/1.1f);
                Vector4 bridges=new Vector4(BridgeVisibility(pair.Key,0),BridgeVisibility(pair.Key,1),BridgeVisibility(pair.Key,2),BridgeVisibility(pair.Key,3));
                if(alpha==c.appliedReveal&&bridges==c.appliedBridges)continue;
                appearance.SetFloat("_Reveal",alpha);
                appearance.SetVector("_BridgeReveal",bridges);
                for (int i = 0; i < 6; i++) c.renderers[i].SetPropertyBlock(appearance);
                c.appliedReveal=alpha;c.appliedBridges=bridges;
            }
        }
        public float BridgeVisibility(SkyCityWfc.Coord coord,int direction)
        {
            Chunk a,b;
            if(!chunks.TryGetValue(coord,out a)||SkyCityWfc.Gate(coord,direction,seed)<0)return 0;
            var neighbor=new SkyCityWfc.Coord(coord.x+(direction==1?1:direction==3?-1:0),coord.z+(direction==0?1:direction==2?-1:0));
            if(!chunks.TryGetValue(neighbor,out b))return 0;
            return Mathf.Clamp01((Time.unscaledTime-Mathf.Max(a.appeared,b.appeared))/1.1f);
        }
        void Rebase()
        {
            Vector3 p = view.transform.position;
            if (Mathf.Abs(p.x) < rebaseThreshold && Mathf.Abs(p.z) < rebaseThreshold) return;
            long x = (long)Math.Floor(p.x/SkyCityWfc.ChunkSize), z = (long)Math.Floor(p.z/SkyCityWfc.ChunkSize);
            OriginX += x; OriginZ += z;
            Vector3 shift = new Vector3(x*SkyCityWfc.ChunkSize,0,z*SkyCityWfc.ChunkSize);
            view.transform.position -= shift;
            foreach (var c in chunks.Values) c.root.transform.position = Position(c.layout.coord);
            OriginShifts++; nextPlan = 0; UpdateCloudOrigin();
        }
        void UpdateCloudOrigin()
        {
            // Periodic noise coordinates stay small without moving the visible cloud field.
            Shader.SetGlobalVector("_SkyWorldOffset",new Vector4((OriginX%500)*SkyCityWfc.ChunkSize,0,(OriginZ%500)*SkyCityWfc.ChunkSize,0));
        }
        public void Teleport(SkyCityWfc.Coord district, Vector3 localPosition)
        {
            OriginX = district.x; OriginZ = district.z; view.transform.position = localPosition;
            foreach (var c in chunks.Values) c.root.transform.position = Position(c.layout.coord);
            UpdateCloudOrigin(); nextPlan = 0;
        }
        public bool TryGetFingerprint(SkyCityWfc.Coord coord, out ulong hash)
        { Chunk chunk; if (chunks.TryGetValue(coord,out chunk)) { hash=chunk.layout.fingerprint;return true; } hash=0;return false; }
        public void ResetTimingPeaks() { PeakMainThreadMs=0;PeakUploadMs=0; }
        void OnDestroy()
        {
            if (lifetime != null) { lifetime.Cancel(); lifetime.Dispose(); }
            foreach (var j in jobs)
            {
                j.cancellation.Cancel(); j.task.ContinueWith(t => { var ignored=t.Exception; j.cancellation.Dispose(); },TaskScheduler.Default);
            }
            jobs.Clear(); if (staging != null) AbortStaging();
            foreach (var c in chunks.Values) foreach (var f in c.filters) if (f.sharedMesh != null) Destroy(f.sharedMesh);
            chunks.Clear(); library=null;
            Shader.SetGlobalVector("_SkyWorldOffset",Vector4.zero); if(Application.isPlaying){Application.targetFrameRate=previousTargetFrameRate;QualitySettings.vSyncCount=previousVsync;}
        }
        void OnGUI()
        {
            if (!Ready || CompletedChunks == 0)
                GUI.Label(new Rect(28,Screen.height-62,520,28),Status + "...");
            else if (Time.realtimeSinceStartupAsDouble-startedAt < 14)
                GUI.Label(new Rect(28,Screen.height-52,1000,32),"W A S D  travel    Right drag  look    Q / E  descend / rise    Shift  faster    Space  cruise    F  return    M  music");
            if (!showDiagnostics) return;
            GUI.Box(new Rect(20,20,390,154),"Streaming diagnostics  |  H to hide");
            GUI.Label(new Rect(32,48,365,130),"Resident " + ResidentCount + "/" + maximumResidentChunks + "    Jobs " + PendingCount + "    Pool " + PoolCount +
                "\nLoaded " + CompletedChunks + "    Unloaded " + UnloadedChunks + "    Rebases " + OriginShifts +
                "\nStream CPU " + LastMainThreadMs.ToString("F2") + " ms    Upload peak " + PeakUploadMs.ToString("F2") + " ms" +
                "\nMesh " + (MeshBytes/1048576f).ToString("F1") + " MiB    Vocabulary " + (LibraryBytes/1048576f).ToString("F1") + " MiB" +
                "\nCoordinate " + (view==null?"-":CurrentCoord().ToString()) + "    Seed " + seed);
        }
    }
}
