using UnityEngine;

namespace SkyCity.Runtime
{
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class SkyCitySoundscape : MonoBehaviour
    {
        [Range(0,1)] public float musicVolume=.48f;
        public float fadeSeconds=3;
        public bool Muted {get;private set;}
        AudioSource music;
        void Start()
        {
            music=GetComponent<AudioSource>();music.loop=true;music.spatialBlend=0;music.volume=0;
            if(music.clip!=null)music.Play();
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.M))ToggleMute();
            if(music!=null)music.volume=Mathf.MoveTowards(music.volume,Muted?0:musicVolume,Time.unscaledDeltaTime*musicVolume/Mathf.Max(.1f,fadeSeconds));
        }
        public void ToggleMute(){Muted=!Muted;}
    }
}
