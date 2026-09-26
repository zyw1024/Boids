using System;
using System.Collections.Generic;
using UnityEngine;
using Boids.Art.Infinite;

namespace Boids.Art
{
    /// <summary>Authored planting slots in the Blender module vocabulary, transformed exactly like its meshes.</summary>
    public static class SkyCityDistrictBotanyLayout
    {
        public struct Slot
        {
            public int cell,seed;public Vector3 position,scale;public Quaternion rotation;
            public bool tree;public bool raisedBed;
        }
        public static List<Slot> Create(SkyCityWfc.Result layout)
        {
            var candidates=new List<Slot>();var shape=SkyCityModuleData.DistrictScale(layout.composition);
            for(int cell=0;cell<SkyCityWfc.CellCount;cell++)
            {
                int x=cell%8,z=cell/8,state=layout.states[cell];
                if(x==0||x==7||z==0||z==7||state==SkyCityWfc.Empty||SkyCityWfc.Sanctuary(cell)||SkyCityWfc.Waterway(cell))continue;
                int family=state/32,variant=state/4%8,turn=state%4;
                Vector3 p;Vector2 size;bool tree=false,raised=true;
                // Slots match build_modules.py. Avoid variant stair/chapel/pergola additions,
                // the central circulation lanes, pools and existing tree trunks.
                // Stay inside the 8.9 m deck and beyond the end of the arcade.
                if(family==0){p=new Vector3(-3.85f,.57f,-3.5f);size=new Vector2(.9f,1.3f);tree=true;}
                // The south arcade is at z=-3; its last column occupies x≈2.4.
                // This pocket lies between that arcade and the east route (|z|<=1.2).
                else if(family==7&&(variant==0||variant==4||variant==6)){p=new Vector3(3.25f,.57f,-1.85f);size=new Vector2(1.65f,.85f);tree=true;}
                else if(family==8){p=new Vector3(-3,1.04f,2.8f);size=new Vector2(2.05f,1.85f);raised=false;}
                else continue;
                var rotation=Quaternion.Euler(0,turn*90,0);
                var local=rotation*p+new Vector3(x*SkyCityWfc.TileSize,SkyCityWfc.Elevation(layout.composition),z*SkyCityWfc.TileSize);
                local=new Vector3(42+(local.x-42)*shape.x,local.y,42+(local.z-42)*shape.y);
                var scale=new Vector3(size.x/2.4f*(turn%2==0?shape.x:shape.y),1,size.y/1.5f*(turn%2==0?shape.y:shape.x));
                candidates.Add(new Slot{cell=cell,position=local,rotation=rotation,scale=scale,tree=tree,raisedBed=raised,seed=unchecked((int)SkyCityWfc.Hash(layout.coord.x*64+cell,layout.coord.z,layout.seed))});
            }
            candidates.Sort((a,b)=>((uint)a.seed).CompareTo((uint)b.seed));
            if(candidates.Count>4)candidates.RemoveRange(4,candidates.Count-4);
            int trees=0;for(int i=0;i<candidates.Count;i++){var slot=candidates[i];if(slot.tree&&++trees>2)slot.tree=false;candidates[i]=slot;}
            return candidates;
        }
    }
}
