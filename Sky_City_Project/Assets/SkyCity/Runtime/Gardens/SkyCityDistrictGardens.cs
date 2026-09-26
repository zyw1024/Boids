using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using SkyCity.Runtime.WorldGeneration;

namespace SkyCity.Runtime
{
    /// <summary>Bounded live plants with session-only state files for unloaded districts.</summary>
    [DisallowMultipleComponent]
    public sealed class SkyCityDistrictGardens : MonoBehaviour
    {
        public SkyCityInfiniteWorld world;
        public SkyCityGardenDirector director;
        public Mesh flowersMesh,cellsMesh,boxMesh;
        public Mesh[] treeMeshes;
        public Material flowersMaterial,treeMaterial,stoneMaterial,soilMaterial;
        public int ResidentDistricts => resident.Count;
        public int LiveGardens {get{int n=0;foreach(var d in resident.Values)n+=d.gardens.Count;return n;}}
        public int LiveTrees {get{int n=0;foreach(var d in resident.Values)n+=d.trees.Count;return n;}}
        public int SavedDistricts {get;private set;}
        public int RestoredDistricts {get;private set;}
        public int PeakDistricts {get;private set;}
        public int PendingDistricts => pending.Count;
        public bool TreeMeshesMatchDepth
        {get{int depth=Mathf.Clamp(director.branchDepth-3,0,2);foreach(var d in resident.Values)foreach(var t in d.trees)if(t.sharedMesh!=treeMeshes[depth])return false;return LiveTrees>0;}}
        public bool PlantMeshesAreShared
        {get{foreach(var d in resident.Values)foreach(var g in d.gardens)if(g.flowers.GetComponent<MeshFilter>().sharedMesh!=flowersMesh||g.rules.GetComponent<MeshFilter>().sharedMesh!=cellsMesh)return false;return LiveGardens>0;}}
        public string StateDirectory {get;private set;}
        sealed class District
        {
            public Transform root;public ulong fingerprint;public int seed;
            public readonly List<SkyCityLivingGarden> gardens=new List<SkyCityLivingGarden>();
            public readonly List<MeshFilter> trees=new List<MeshFilter>();
            public readonly List<Renderer> treeRenderers=new List<Renderer>();
            public int depth=-1;public float breeze=-1,growth=-1;
        }
        struct Request {public SkyCityWfc.Result layout;public Transform root;public int lod;}
        [Serializable] sealed class Snapshot {public SkyCityLivingGarden.State[] gardens;}
        readonly Dictionary<SkyCityWfc.Coord,District> resident=new Dictionary<SkyCityWfc.Coord,District>();
        readonly Dictionary<SkyCityWfc.Coord,Request> pending=new Dictionary<SkyCityWfc.Coord,Request>();
        MaterialPropertyBlock treeProperties;
        bool shuttingDown;
        void Awake()
        {
            if(world==null)world=GetComponent<SkyCityInfiniteWorld>();
            StateDirectory=Path.Combine(Application.temporaryCachePath,"SkyCityGardenSessions",Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(StateDirectory);treeProperties=new MaterialPropertyBlock();
            world.DistrictReady+=Ready;world.DistrictUnloading+=Unload;
        }
        void Ready(SkyCityWfc.Result layout,Transform root,int lod)
        {
            District existing;
            if(resident.TryGetValue(layout.coord,out existing)&&existing.fingerprint==layout.fingerprint&&existing.seed==layout.seed&&existing.root.parent==root)return;
            pending[layout.coord]=new Request{layout=layout,root=root,lod=lod};
        }
        void Update()
        {
            if(director==null)return;
            // The authored arrival district may be disabled hundreds of metres away.
            if(!director.isActiveAndEnabled)director.TickControls();
            if(pending.Count>0)
            {
                var item=default(KeyValuePair<SkyCityWfc.Coord,Request>);foreach(var candidate in pending){item=candidate;break;}
                pending.Remove(item.Key);
                if(item.Value.root!=null&&item.Value.root.gameObject.activeInHierarchy)Build(item.Value);
            }
            foreach(var district in resident.Values)
            {
                bool visible=(district.root.position+new Vector3(42,12,42)-world.view.transform.position).sqrMagnitude<260*260;
                if(district.root.gameObject.activeSelf!=visible)district.root.gameObject.SetActive(visible);
                int depth=Mathf.Clamp(director.branchDepth-3,0,2);
                if(district.depth==depth&&district.breeze==director.breeze&&district.growth==director.TreeGrowth)continue;
                treeProperties.SetFloat("_Growth",director.TreeGrowth);treeProperties.SetFloat("_Generations",director.branchDepth+1);treeProperties.SetFloat("_Breeze",director.breeze);
                foreach(var tree in district.trees)tree.sharedMesh=treeMeshes[depth];
                foreach(var renderer in district.treeRenderers)renderer.SetPropertyBlock(treeProperties);
                district.depth=depth;district.breeze=director.breeze;district.growth=director.TreeGrowth;
            }
        }
        void Build(Request request)
        {
            Unload(request.layout.coord);
            var go=new GameObject("Living district gardens");go.SetActive(false);go.transform.SetParent(request.root,false);
            var district=new District{root=go.transform,fingerprint=request.layout.fingerprint,seed=request.layout.seed};
            foreach(var slot in SkyCityDistrictBotanyLayout.Create(request.layout))
            {
                var bed=new GameObject("Living planter cell "+slot.cell);bed.transform.SetParent(go.transform,false);
                bed.transform.localPosition=slot.position;bed.transform.localRotation=slot.rotation;bed.transform.localScale=slot.scale;
                if(slot.raisedBed)
                {
                    var stone=MeshObject(bed.transform,"Limestone planter",boxMesh,stoneMaterial);
                    stone.transform.localPosition=new Vector3(0,-.18f,0);stone.transform.localScale=new Vector3(2.55f,.32f,1.65f);
                }
                var soil=MeshObject(bed.transform,"Recessed planting soil",boxMesh,soilMaterial);
                soil.transform.localPosition=new Vector3(0,-.025f,0);soil.transform.localScale=new Vector3(2.4f,.045f,1.5f);
                var garden=bed.AddComponent<SkyCityLivingGarden>();garden.director=director;garden.columns=12;garden.rows=4;
                garden.size=new Vector2(2.4f,1.5f);garden.continuous=true;garden.bedSpacing=2.4f;garden.seed=slot.seed;
                garden.flowers=MeshObject(bed.transform,"Living lavender and ivory",flowersMesh,flowersMaterial);
                garden.rules=MeshObject(bed.transform,"Cell states",cellsMesh,flowersMaterial);garden.rules.shadowCastingMode=ShadowCastingMode.Off;
                garden.Initialize();director.Register(garden);district.gardens.Add(garden);
                if(slot.tree)
                {
                    var tree=MeshObject(bed.transform,"Recursive courtyard tree",treeMeshes[2],treeMaterial);
                    tree.transform.localScale=Vector3.one*(.22f+((uint)slot.seed%4)*.025f);
                    tree.transform.localRotation=Quaternion.Euler(0,slot.treeYaw,0);
                    district.trees.Add(tree.GetComponent<MeshFilter>());district.treeRenderers.Add(tree);
                }
            }
            string file=StatePath(request.layout.coord,district);
            if(File.Exists(file))
            {
                var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(file));
                if(snapshot!=null&&snapshot.gardens.Length==district.gardens.Count)
                {for(int i=0;i<snapshot.gardens.Length;i++)district.gardens[i].RestoreState(snapshot.gardens[i]);RestoredDistricts++;}
            }
            resident.Add(request.layout.coord,district);PeakDistricts=Math.Max(PeakDistricts,resident.Count);go.SetActive(true);
        }
        static MeshRenderer MeshObject(Transform parent,string name,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.On;return renderer;
        }
        string StatePath(SkyCityWfc.Coord coord,District d){return Path.Combine(StateDirectory,coord.x+"_"+coord.z+"_"+d.seed+"_"+d.fingerprint.ToString("x16")+".json");}
        void Unload(SkyCityWfc.Coord coord)
        {
            pending.Remove(coord);District district;if(!resident.TryGetValue(coord,out district))return;
            if(!shuttingDown)
            {
                var snapshot=new Snapshot{gardens=new SkyCityLivingGarden.State[district.gardens.Count]};
                for(int i=0;i<snapshot.gardens.Length;i++)snapshot.gardens[i]=district.gardens[i].CaptureState();
                string file=StatePath(coord,district);if(!File.Exists(file))SavedDistricts++;
                File.WriteAllText(file,JsonUtility.ToJson(snapshot));
            }
            foreach(var garden in district.gardens)director.Unregister(garden);
            district.root.gameObject.SetActive(false);Destroy(district.root.gameObject);resident.Remove(coord);
        }
        public SkyCityLivingGarden[] GardensAt(SkyCityWfc.Coord coord)
        {District d;return resident.TryGetValue(coord,out d)?d.gardens.ToArray():new SkyCityLivingGarden[0];}
        public SkyCityWfc.Coord[] Coordinates {get{var result=new SkyCityWfc.Coord[resident.Count];resident.Keys.CopyTo(result,0);return result;}}
        void OnGUI(){if(director!=null&&!director.isActiveAndEnabled)director.DrawHints();}
        void OnDestroy()
        {
            shuttingDown=true;if(world!=null){world.DistrictReady-=Ready;world.DistrictUnloading-=Unload;}
            foreach(var district in resident.Values)foreach(var garden in district.gardens)if(director!=null)director.Unregister(garden);
            resident.Clear();pending.Clear();
            // This directory belongs exclusively to this instance and contains only session snapshots.
            string sessionBase=Path.GetFullPath(Path.Combine(Application.temporaryCachePath,"SkyCityGardenSessions"))+Path.DirectorySeparatorChar;
            if(StateDirectory!=null&&Path.GetFullPath(StateDirectory).StartsWith(sessionBase,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(StateDirectory))Directory.Delete(StateDirectory,true);
        }
    }
}
