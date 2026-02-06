using System;

namespace Script.CommonLib
{
    [Serializable]
    public class EntityData
    {
        // TODO: 추후에 더 좋은 방법으로 수정하면 좋을 것 같음. (예: startPosition, endPosition을 이름으로 찾아오고 있는 부분)
        public string name;
        public string startPositionName;
        public string endPositionName;
    }
}
