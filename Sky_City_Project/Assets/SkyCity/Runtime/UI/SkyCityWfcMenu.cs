using System.Collections.Generic;
using SkyCity.Runtime.WorldGeneration;
using UnityEngine;
using UnityEngine.UI;

namespace SkyCity.Runtime
{
    public sealed partial class SkyCityFlockMenu
    {
        RectTransform flockPage,worldPage;
        Button flockTab,worldTab,applyWorld;
        InputField seedInput;
        SkyCityInfiniteWorld world;
        SkyCityWfc.Settings draftSettings,initialSettings;
        int initialSeed,initialRadius;
        float initialBudget;
        readonly List<Binding> worldBindings=new List<Binding>();
        public bool WorldPageActive {get;private set;}
        public bool HasWorldChanges
        {get{int seed;return world!=null&&int.TryParse(seedInput.text,out seed)&&(seed!=world.seed||!draftSettings.Equals(world.ActiveSettings));}}

        static RectTransform Page(Transform parent,string name)
        {
            var page=new GameObject(name,typeof(RectTransform));page.transform.SetParent(parent,false);
            var rect=(RectTransform)page.transform;Stretch(rect);return rect;
        }
        void BuildWorldPage(Transform parent)
        {
            world=player.world;worldPage=Page(parent,"WFC controls");
            worldTab.interactable=world!=null;if(world==null)return;
            initialSeed=world.seed;draftSettings=initialSettings=world.ActiveSettings;
            initialRadius=world.loadRadius;initialBudget=world.mainThreadBudgetMilliseconds;
            Label(worldPage,"随机种子",26,172,300,22,17,Paper);
            var background=Image("WorldSeed",worldPage,new Color(.14f,.23f,.26f));Place(background.rectTransform,26,202,306,38);
            seedInput=background.gameObject.AddComponent<InputField>();seedInput.targetGraphic=background;
            seedInput.textComponent=Label(background.transform,"",12,0,282,38,18,Paper);
            seedInput.contentType=InputField.ContentType.IntegerNumber;seedInput.characterLimit=11;
            seedInput.navigation=new Navigation{mode=Navigation.Mode.None};seedInput.text=initialSeed.ToString();
            seedInput.onValueChanged.AddListener(_=>UpdateWorldStatus());
            Button(worldPage,"Randomize seed","换个种子",344,202,110,false,()=>seedInput.text=Random.Range(1,int.MaxValue).ToString(),38);
            Row(worldPage,"BridgeProbability","桥梁连接概率",256,0,1,()=>draftSettings.bridgeProbability,v=>draftSettings.bridgeProbability=v,"P0","",false,worldBindings);
            Row(worldPage,"ShoreCompleteness","岛缘完整度",314,.35f,1,()=>draftSettings.shoreCompleteness,v=>draftSettings.shoreCompleteness=v,"P0","",false,worldBindings);
            Row(worldPage,"GardenWeight","花园模块权重",372,.2f,4,()=>draftSettings.gardenWeight,v=>draftSettings.gardenWeight=v,"0.0"," ×",false,worldBindings);
            Row(worldPage,"TowerWeight","高塔模块权重",430,.2f,4,()=>draftSettings.towerWeight,v=>draftSettings.towerWeight=v,"0.0"," ×",false,worldBindings);
            Row(worldPage,"VariantCoherence","区域风格统一度",488,1,4,()=>draftSettings.variantCoherence,v=>draftSettings.variantCoherence=v,"0.0"," ×",false,worldBindings);
            Row(worldPage,"WfcAttempts","求解尝试上限",546,1,8,()=>draftSettings.attempts,v=>draftSettings.attempts=Mathf.RoundToInt(v),"0"," 次",true,worldBindings);
            Section(worldPage,"流式加载",610);
            Row(worldPage,"LoadRadius","加载半径",640,1,3,()=>world.loadRadius,v=>world.SetStreaming(Mathf.RoundToInt(v),world.mainThreadBudgetMilliseconds),"0"," 圈",true,worldBindings);
            Row(worldPage,"FrameBudget","每帧生成预算",698,.5f,4,()=>world.mainThreadBudgetMilliseconds,v=>world.SetStreaming(world.loadRadius,v),"0.0"," ms",false,worldBindings);
            Label(worldPage,"生成参数需应用；加载设置即时生效。\n加载 1 / 2 / 3 圈，上限为 9 / 25 / 49 个街区。",26,758,428,42,13,Muted);
            applyWorld=Button(worldPage,"Apply world","应用并更新街区",26,808,428,true,ApplyWorldSettings);
            Button(worldPage,"Reset world","恢复默认参数",26,862,190,false,ResetWorldDefaults);
            Button(worldPage,"Resume from world","继续飞行  Esc",228,862,226,true,()=>SetOpen(false));
            Label(worldPage,"周边街区分批更新，主岛与建筑连接约束保留。",26,921,428,22,13,Muted);
        }
        public void ShowWorldSettings(bool value)
        {
            GardenPageActive=false;if(gardenPage!=null)gardenPage.gameObject.SetActive(false);if(gardenTab!=null)StyleTab(gardenTab,false);
            WorldPageActive=value&&world!=null;flockPage.gameObject.SetActive(!WorldPageActive);worldPage.gameObject.SetActive(WorldPageActive);
            StyleTab(flockTab,!WorldPageActive);StyleTab(worldTab,WorldPageActive);
            if(WorldPageActive){foreach(var binding in worldBindings)binding.Refresh();UpdateWorldStatus();}
            nextStatus=0;
        }
        static void StyleTab(Button button,bool selected)
        {
            button.GetComponent<Image>().color=selected?Teal:new Color(.17f,.26f,.28f);
            button.GetComponentInChildren<Text>().color=selected?Ink:Paper;
        }
        void UpdateWorldStatus()
        {
            if(world==null||applyWorld==null)return;
            int seed;bool valid=int.TryParse(seedInput.text,out seed);
            applyWorld.interactable=valid&&world.Ready&&HasWorldChanges;
            if(!WorldPageActive)return;
            status.text=!valid?"请输入有效整数种子":HasWorldChanges?"有待应用的生成参数":
                !world.GenerationComplete?"正在更新街区 · "+world.CurrentGenerationChunks+" / "+world.TargetChunks:
                "世界已更新 · "+world.ResidentCount+" 个街区 · 种子 "+world.seed;
        }
        public void ApplyWorldSettings()
        {
            int seed;if(!int.TryParse(seedInput.text,out seed)||world==null||!world.Ready)return;
            draftSettings=draftSettings.Normalized();world.ApplyGeneration(seed,draftSettings);UpdateWorldStatus();
        }
        public void ResetWorldDefaults()
        {
            if(world==null)return;
            draftSettings=initialSettings;seedInput.text=initialSeed.ToString();world.SetStreaming(initialRadius,initialBudget);
            foreach(var binding in worldBindings)binding.Refresh();UpdateWorldStatus();
        }
    }
}
