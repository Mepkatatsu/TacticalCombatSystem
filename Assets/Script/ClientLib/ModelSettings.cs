using System;
using System.Collections.Generic;
using Script.CommonLib;

namespace Script.ClientLib
{
    public class ModelSettings : ScriptableObjectBase<ModelSettings>
    {
        [Serializable]
        public class ModelData
        {
            public string entityName;
            public string modelPath;
            public Vec3 modelScale = new Vec3(3.5f, 3.5f, 3.5f);
        }
        
        public List<ModelData> modelPaths;

        public ModelData GetModelData(string entityName)
        {
            // TODO: 성능 개선, 비슷한 방식이 자주 사용될 것 같아 기반을 다져놓으면 좋을 것 같음 (예를 들면 ModelPathData를 기반으로 entityName을 key로 가지는 Dictionary로 자동 변환하여 찾아올 수 있는 자료 구조를 만든다든가)
            return modelPaths.Find(e => e.entityName == entityName);
        }
    }
}
