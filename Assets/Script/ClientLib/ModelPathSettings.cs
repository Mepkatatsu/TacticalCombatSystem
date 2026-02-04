using System;
using System.Collections.Generic;

namespace Script.ClientLib
{
    public class ModelPathSettings : ScriptableObjectBase<ModelPathSettings>
    {
        [Serializable]
        public class ModelPathData
        {
            public string entityName;
            public string modelPath;
        }
        
        public List<ModelPathData> modelPaths;

        public string GetModelPath(string entityName)
        {
            // TODO: 성능 개선, 비슷한 방식이 자주 사용될 것 같아 기반을 다져놓으면 좋을 것 같음 (예를 들면 ModelPathData를 기반으로 entityName을 key로 가지는 Dictionary로 자동 변환하여 찾아올 수 있는 자료 구조를 만든다든가)
            return modelPaths.Find(e => e.entityName == entityName)?.modelPath;
        }
    }
}
