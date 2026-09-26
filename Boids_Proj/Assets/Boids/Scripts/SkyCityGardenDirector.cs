using UnityEngine;

namespace Boids.Art
{
    public sealed class SkyCityGardenDirector : MonoBehaviour
    {
        public SkyCityFirstPerson player;
        public SkyCityLivingGarden[] gardens;
        public GameObject[] treeGenerations;
        public int branchDepth=5,neighbours=1;
        public float evolutionSpeed=1,budSeconds=1.4f,bloomSeconds=9,recoverySeconds=16,breeze=1;
        public bool showRules;
        public bool windSeeds=true;
        public float TreeGrowth {get;private set;}=1;
        public int Plantings {get;private set;}
        public bool Growing => TreeGrowth<1;
        Renderer[] treeRenderers;
        MaterialPropertyBlock treeProperties;
        float noticeUntil;
        string notice;
        public int Blooms {get{int n=0;if(gardens!=null)foreach(var g in gardens)if(g.Simulation!=null)n+=g.Simulation.BloomCount;return n;}}
        void Awake()
        {
            treeProperties=new MaterialPropertyBlock();
            treeRenderers=GetComponentsInChildren<Renderer>(true);
            ApplyDepth(branchDepth);RefreshTree();
        }
        void Update()
        {
            if(Growing){TreeGrowth=Mathf.Min(1,TreeGrowth+Time.deltaTime/16);RefreshTree();}
            if(player==null||!player.acceptInput||!player.Captured||(player.flockMenu!=null&&player.flockMenu.IsOpen))return;
            if(Input.GetKeyDown(KeyCode.G))TryPlant();
        }
        public bool TryPlant()
        {
            if(player==null||(player.flockMenu!=null&&player.flockMenu.IsOpen))return false;
            var ray=new Ray(player.view.transform.position,player.view.transform.forward);Vector3 point;
            foreach(var garden in gardens)
                if(garden.isActiveAndEnabled&&garden.SowRay(ray,out point))
                {Plantings++;notice="A seed awakens the garden";noticeUntil=Time.unscaledTime+3;if(player.flock!=null)player.flock.VisitGarden(point);return true;}
            notice="Aim at a planted terrace within 45 m";noticeUntil=Time.unscaledTime+3;return false;
        }
        public void AwakenGardens()
        {
            foreach(var g in gardens)for(float u=g.bedSpacing*.5f;u<g.size.x;u+=g.bedSpacing)g.Sow(Mathf.Clamp((int)(u/g.size.x*g.columns),0,g.columns-1),g.rows/2);
            if(player!=null&&player.flock!=null&&gardens.Length>0)player.flock.VisitGarden(gardens[0].transform.position);
        }
        public void ReplayTree(){TreeGrowth=0;RefreshTree();}
        public void ApplyDepth(int value)
        {branchDepth=Mathf.Clamp(value,3,5);if(treeGenerations!=null)for(int i=0;i<treeGenerations.Length;i++)treeGenerations[i].SetActive(i==branchDepth-3);RefreshTree();}
        void RefreshTree()
        {
            if(treeProperties==null||treeRenderers==null)return;
            treeProperties.SetFloat("_Growth",TreeGrowth);treeProperties.SetFloat("_Generations",branchDepth+1);treeProperties.SetFloat("_Breeze",breeze);
            foreach(var r in treeRenderers)if(r!=null&&r.GetComponentInParent<SkyCityLivingGarden>()==null)r.SetPropertyBlock(treeProperties);
        }
        public void RefreshAppearance(){RefreshTree();foreach(var g in gardens)g.RefreshAppearance();}
        public void ResetDefaults()
        {evolutionSpeed=1;budSeconds=1.4f;bloomSeconds=9;recoverySeconds=16;breeze=1;neighbours=1;showRules=false;windSeeds=true;TreeGrowth=1;ApplyDepth(5);RefreshAppearance();}
        void OnGUI()
        {
            if(player==null||!player.acceptInput||(player.flockMenu!=null&&player.flockMenu.IsOpen))return;
            var style=new GUIStyle(GUI.skin.label){fontSize=14};style.normal.textColor=new Color(.95f,.91f,.79f);
            GUI.Label(new Rect(26,Screen.height-86,1100,24),"G  sow a seed in the terrace gardens",style);
            if(Time.unscaledTime<noticeUntil)GUI.Label(new Rect(Screen.width*.5f-180,Screen.height*.66f,450,30),notice,style);
        }
    }
}
