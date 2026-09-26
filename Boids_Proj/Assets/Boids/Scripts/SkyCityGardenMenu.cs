using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Boids.Art
{
    public sealed partial class SkyCityFlockMenu
    {
        RectTransform gardenPage;
        Button gardenTab,rulesButton,windSeedsButton;
        SkyCityGardenDirector garden;
        readonly List<Binding> gardenBindings=new List<Binding>();
        public bool GardenPageActive {get;private set;}
        void BuildGardenPage(Transform parent)
        {
            garden=FindObjectOfType<SkyCityGardenDirector>();gardenPage=Page(parent,"Garden controls");gardenTab.interactable=garden!=null;
            if(garden==null)return;
            Section(gardenPage,"01   风之树 · 分形",172);
            Row(gardenPage,"BranchDepth","递归分枝层数",204,3,5,()=>garden.branchDepth,v=>garden.ApplyDepth(Mathf.RoundToInt(v)),"0"," 层",true,gardenBindings);
            Row(gardenPage,"GardenBreeze","枝叶风动",262,0,2,()=>garden.breeze,v=>{garden.breeze=v;garden.RefreshAppearance();},"0.0"," ×",false,gardenBindings);
            Button(gardenPage,"Replay tree","观看树木逐级生长",26,326,428,false,()=>garden.ReplayTree(),38);
            Section(gardenPage,"02   花园 · 元胞自动机",388);
            Row(gardenPage,"GardenSpeed","演化速度",418,.25f,3,()=>garden.evolutionSpeed,v=>garden.evolutionSpeed=v,"0.00"," ×",false,gardenBindings);
            Row(gardenPage,"GardenNeighbours","萌芽所需盛开邻居",476,1,4,()=>garden.neighbours,v=>garden.neighbours=Mathf.RoundToInt(v),"0"," 个",true,gardenBindings);
            Row(gardenPage,"GardenBloomDuration","盛开时间",534,3,20,()=>garden.bloomSeconds,v=>garden.bloomSeconds=v,"0.0"," s",false,gardenBindings);
            Row(gardenPage,"GardenRestDuration","恢复时间",592,4,30,()=>garden.recoverySeconds,v=>garden.recoverySeconds=v,"0.0"," s",false,gardenBindings);
            Button(gardenPage,"Awaken gardens","唤醒花园",26,662,206,true,()=>garden.AwakenGardens(),40);
            rulesButton=Button(gardenPage,"Garden rules","规则着色：关",244,662,210,false,()=>{garden.showRules=!garden.showRules;garden.RefreshAppearance();UpdateGardenStatus();},40);
            Label(gardenPage,"瞄准花坛后按 G 播种；右键仍然呼唤鸟群。\n蓝休眠 → 绿萌芽 → 金盛开 → 紫恢复\n每个格子读取上一代的八个相邻格子。",26,720,428,70,14,Muted);
            Button(gardenPage,"Reset garden states","清空花潮",26,808,206,false,()=>{garden.windSeeds=false;foreach(var g in garden.gardens)g.ResetGarden();UpdateGardenStatus();},38);
            windSeedsButton=Button(gardenPage,"Wind seeding","风播种：开",244,808,210,false,()=>{garden.windSeeds=!garden.windSeeds;UpdateGardenStatus();},38);
            Button(gardenPage,"Reset garden parameters","恢复默认参数",26,862,190,false,()=>{garden.ResetDefaults();foreach(var b in gardenBindings)b.Refresh();UpdateGardenStatus();});
            Button(gardenPage,"Resume from garden","继续飞行  Esc",228,862,226,true,()=>SetOpen(false));
            Label(gardenPage,"附近花园持续演化，远处暂停并保留状态。",26,921,428,22,13,Muted);
        }
        public void ShowGardenSettings()
        {
            if(garden==null)return;
            ShowWorldSettings(false);GardenPageActive=true;flockPage.gameObject.SetActive(false);gardenPage.gameObject.SetActive(true);
            StyleTab(flockTab,false);StyleTab(gardenTab,true);foreach(var b in gardenBindings)b.Refresh();UpdateGardenStatus();
        }
        void UpdateGardenStatus()
        {
            if(garden==null)return;
            status.text=garden.Growing?"风之树正在生长 · "+garden.TreeGrowth.ToString("P0"):"花园正在呼吸 · "+garden.Blooms+" 簇盛开";
            rulesButton.GetComponentInChildren<Text>().text=garden.showRules?"规则着色：开":"规则着色：关";
            if(windSeedsButton!=null)windSeedsButton.GetComponentInChildren<Text>().text=garden.windSeeds?"风播种：开":"风播种：关";
        }
    }
}
