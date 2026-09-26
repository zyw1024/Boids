using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Boids.Art
{
    /// <summary>Live flock controls. The world continues while player input is captured by the menu.</summary>
    public sealed partial class SkyCityFlockMenu : MonoBehaviour
    {
        public Font font;
        public bool IsOpen {get;private set;}
        SkyCityFirstPerson player;
        SkyCityFlock flock;
        GameObject canvasRoot;
        Text status;
        float nextStatus;
        readonly List<Binding> bindings=new List<Binding>();
        static readonly Color Ink=new Color(.067f,.12f,.15f,.96f);
        static readonly Color Paper=new Color(.93f,.92f,.85f);
        static readonly Color Muted=new Color(.57f,.69f,.70f);
        static readonly Color Gold=new Color(.81f,.73f,.48f);
        static readonly Color Teal=new Color(.39f,.71f,.69f);
        sealed class Binding
        {
            public Slider slider;
            public Text value;
            public Func<float> read;
            public Action<float> write;
            public float initial;
            public string format,suffix;
            public void Refresh()
            {
                float current=read();slider.SetValueWithoutNotify(current);
                value.text=current.ToString(format)+suffix;
            }
        }

        void Start()
        {
            player=GetComponent<SkyCityFirstPerson>();flock=player.flock;player.flockMenu=this;
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(EventSystem.current==null)
            {
                var events=new GameObject("Flock menu event system",typeof(EventSystem),typeof(StandaloneInputModule));
                events.transform.SetParent(transform,false);
            }
            canvasRoot=new GameObject("Flock settings UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform,false);
            var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;
            var scaler=canvasRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=1;
            var scrim=Image("Input curtain",canvasRoot.transform,new Color(.015f,.045f,.06f,.16f));Stretch(scrim.rectTransform);
            var shadow=Image("Panel shadow",canvasRoot.transform,new Color(0,0,0,.16f));Place(shadow.rectTransform,56,56,488,962);
            var panel=Image("Flock panel",canvasRoot.transform,Ink);Place(panel.rectTransform,48,48,480,962);
            var accent=Image("Gold rule",panel.transform,Gold);Place(accent.rectTransform,0,0,480,2);
            Label(panel.transform,"SKY CITY   /   FIELD NOTES",26,21,428,20,12,Gold);
            Label(panel.transform,"场景设置",26,49,350,46,34,Paper);
            flockTab=Button(panel.transform,"Flock tab","鸟群",26,103,104,true,()=>ShowWorldSettings(false),34);
            worldTab=Button(panel.transform,"World tab","世界 · WFC",140,103,148,false,()=>ShowWorldSettings(true),34);
            gardenTab=Button(panel.transform,"Garden tab","空中花园",298,103,156,false,ShowGardenSettings,34);
            status=Label(panel.transform,"实时预览 · 64 只飞鸟",28,144,420,20,13,Teal);
            flockPage=Page(panel.transform,"Flock controls");
            Section(flockPage,"01   飞行",166);
            Row(flockPage,"BirdCount","鸟群数量",192,16,128,()=>flock.BirdCount,v=>flock.SetBirdCount(Mathf.RoundToInt(v)),"0"," 只",true);
            Row(flockPage,"CruiseSpeed","飞行速度",250,3,14,()=>flock.cruiseSpeed,v=>flock.cruiseSpeed=v,"0.0"," m/s");
            Row(flockPage,"Steering","转向灵活度",308,6,32,()=>flock.steeringLimit,v=>flock.steeringLimit=v,"0.0","");
            Section(flockPage,"02   群体行为",378);
            Row(flockPage,"NeighbourRadius","感知半径",404,2,12,()=>flock.neighbourRadius,v=>flock.neighbourRadius=v,"0.0"," m");
            Row(flockPage,"SeparationRadius","个体间距",462,.5f,3.5f,()=>flock.separationRadius,v=>flock.separationRadius=v,"0.00"," m");
            Row(flockPage,"SeparationWeight","分离 · 避免拥挤",520,0,8,()=>flock.separationWeight,v=>flock.separationWeight=v,"0.00","");
            Row(flockPage,"AlignmentWeight","对齐 · 同向飞行",578,0,2,()=>flock.alignmentWeight,v=>flock.alignmentWeight=v,"0.00","");
            Row(flockPage,"CohesionWeight","聚合 · 靠近同伴",636,0,.6f,()=>flock.cohesionWeight,v=>flock.cohesionWeight=v,"0.00","");
            Section(flockPage,"03   右键召集",706);
            Row(flockPage,"CallRadius","盘旋半径",732,2,12,()=>flock.callRadius,v=>flock.callRadius=v,"0.0"," m");
            Row(flockPage,"CallDuration","停留时间",790,5,40,()=>flock.callDuration,v=>flock.SetCallDuration(v),"0"," s",true);
            Button(flockPage,"Reset defaults","恢复默认",26,862,190,false,ResetDefaults);
            Button(flockPage,"Resume flight","继续飞行  Esc",228,862,226,true,()=>SetOpen(false));
            Label(flockPage,"参数即时生效 · 场景继续运行",26,921,428,22,13,Muted);
            BuildWorldPage(panel.transform);BuildGardenPage(panel.transform);ShowWorldSettings(false);
            canvasRoot.SetActive(false);
        }

        void Update()
        {
            if(!IsOpen||Time.unscaledTime<nextStatus)return;
            nextStatus=Time.unscaledTime+.25f;
            if(GardenPageActive)UpdateGardenStatus();
            else if(WorldPageActive)UpdateWorldStatus();
            else status.text="实时预览 · "+flock.BirdCount+" 只飞鸟"+(flock.InvitationActive?" · 召集中":" · 自由飞行");
        }
        public void SetOpen(bool open)
        {
            if(canvasRoot==null)return;
            IsOpen=open;canvasRoot.SetActive(open);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
            if(open){foreach(var binding in bindings)binding.Refresh();nextStatus=0;}
            player.CapturePointer(!open&&player.acceptInput);
        }
        public void ResetDefaults()
        {
            foreach(var binding in bindings)binding.write(binding.initial);
            foreach(var binding in bindings)binding.Refresh();
        }
        void OnDisable()
        {
            IsOpen=false;if(canvasRoot!=null)canvasRoot.SetActive(false);
            if(player!=null)player.CapturePointer(false);
        }
        void Section(Transform parent,string title,float y)
        {
            Label(parent,title,26,y,428,20,13,Gold);
            var line=Image(title+" rule",parent,new Color(Muted.r,Muted.g,Muted.b,.18f));Place(line.rectTransform,145,y+11,309,1);
        }
        void Row(Transform parent,string id,string title,float y,float min,float max,Func<float> read,Action<float> write,string format,string suffix,bool integer=false,List<Binding> target=null)
        {
            Label(parent,title,26,y,294,24,17,Paper);
            var value=Label(parent,"",321,y,133,24,17,Teal);value.alignment=TextAnchor.MiddleRight;
            var root=new GameObject(id,typeof(RectTransform),typeof(Slider));root.transform.SetParent(parent,false);
            Place((RectTransform)root.transform,26,y+25,428,28);
            var slider=root.GetComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.wholeNumbers=integer;
            slider.navigation=new Navigation{mode=Navigation.Mode.None};
            // The transparent hit area makes the entire row-height track draggable.
            var hit=Image("Hit area",root.transform,Color.clear);Stretch(hit.rectTransform);
            var track=Image("Track",root.transform,new Color(.22f,.31f,.33f));Place(track.rectTransform,0,12,428,3);
            var fillArea=new GameObject("Fill area",typeof(RectTransform));fillArea.transform.SetParent(root.transform,false);Place((RectTransform)fillArea.transform,0,12,428,3);
            var fill=Image("Fill",fillArea.transform,Teal);Stretch(fill.rectTransform);
            var handleArea=new GameObject("Handle area",typeof(RectTransform));handleArea.transform.SetParent(root.transform,false);Place((RectTransform)handleArea.transform,0,14,428,0);
            var handle=Image("Handle",handleArea.transform,Paper);handle.rectTransform.sizeDelta=new Vector2(12,16);handle.rectTransform.pivot=new Vector2(.5f,.5f);
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.direction=Slider.Direction.LeftToRight;
            var colours=slider.colors;colours.normalColor=Color.white;colours.highlightedColor=Gold;colours.pressedColor=Teal;colours.fadeDuration=.12f;slider.colors=colours;
            var binding=new Binding{slider=slider,value=value,read=read,write=write,initial=read(),format=format,suffix=suffix};
            binding.Refresh();(target??bindings).Add(binding);
            slider.onValueChanged.AddListener(v=>{binding.write(v);binding.Refresh();});
        }
        Button Button(Transform parent,string id,string caption,float x,float y,float width,bool primary,Action clicked,float height=46)
        {
            var background=Image(id,parent,primary?Teal:new Color(.17f,.26f,.28f));Place(background.rectTransform,x,y,width,height);
            var button=background.gameObject.AddComponent<Button>();button.targetGraphic=background;button.navigation=new Navigation{mode=Navigation.Mode.None};
            var text=Label(background.transform,caption,0,0,width,height,16,primary?Ink:Paper);text.alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(()=>clicked());
            return button;
        }
        Text Label(Transform parent,string caption,float x,float y,float width,float height,int size,Color color)
        {
            var root=new GameObject(caption.Length>0?caption:"Value",typeof(RectTransform),typeof(Text));root.transform.SetParent(parent,false);
            var text=root.GetComponent<Text>();text.font=font;text.text=caption;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleLeft;
            text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;
            Place(text.rectTransform,x,y,width,height);return text;
        }
        static Image Image(string name,Transform parent,Color color)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Image));root.transform.SetParent(parent,false);
            var image=root.GetComponent<Image>();image.color=color;return image;
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
        static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
