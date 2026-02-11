using System;
using System.Collections.Generic;
using Script.CommonLib;

namespace Script.ClientLib
{
    public class ProjectileSettings : ScriptableObjectBase<ProjectileSettings>
    {
        [Serializable]
        public class ProjectileData
        {
            public string projectileName;
            public string projectilePath;
            public Vec3 scale = new Vec3(1f, 1f, 1f);
        }
        
        public List<ProjectileData> dataList;

        public ProjectileData GetProjectileData(string projectileName)
        {
            // TODO: 성능 개선, 비슷한 방식이 자주 사용될 것 같아 기반을 다져놓으면 좋을 것 같음
            return dataList.Find(e => e.projectileName == projectileName);
        }
    }
}
