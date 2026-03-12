using System;

namespace Script.CommonLib
{
    [Serializable]
    public struct FixedDir
    {
        public FixedPos currentFixedPos;
        public FixedPos targetFixedPos;

        public FixedDir(FixedPos currentFixedPos, FixedPos targetFixedPos)
        {
            this.currentFixedPos = currentFixedPos;
            this.targetFixedPos = targetFixedPos;
        }

        public override string ToString()
        {
            return $"current:{currentFixedPos}, target:{targetFixedPos}";
        }
    }
}
