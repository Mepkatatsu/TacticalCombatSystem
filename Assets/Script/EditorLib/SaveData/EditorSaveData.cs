using Script.ClientLib.SaveData;

namespace Script.EditorLib.SaveData
{
    public static class EditorSaveData
    {
        public static bool DrawBattleMapBaseGrid
        {
            get => UnitySaveData.GetData(nameof(DrawBattleMapBaseGrid), false);
            set
            {
                var original = UnitySaveData.GetData(nameof(DrawBattleMapBaseGrid), false);
                
                if (value != original)
                    UnityEditor.SceneView.RepaintAll();
                
                UnitySaveData.SetData(nameof(DrawBattleMapBaseGrid), value);
            }
        }

        public static bool DrawObstacle
        {
            get => UnitySaveData.GetData(nameof(DrawObstacle), false);
            set
            {
                var original = UnitySaveData.GetData(nameof(DrawObstacle), false);
                
                if (value != original)
                    UnityEditor.SceneView.RepaintAll();
                
                UnitySaveData.SetData(nameof(DrawObstacle), value);
            }
        }
        
        public static bool DrawBattlePositions
        {
            get => UnitySaveData.GetData(nameof(DrawBattlePositions), false);
            set
            {
                var original = UnitySaveData.GetData(nameof(DrawBattlePositions), false);
                
                if (value != original)
                    UnityEditor.SceneView.RepaintAll();
                
                UnitySaveData.SetData(nameof(DrawBattlePositions), value);
            }
        }
        
        public static bool ShowPathFinder
        {
            get => UnitySaveData.GetData(nameof(ShowPathFinder), false);
            set
            {
                var original = UnitySaveData.GetData(nameof(ShowPathFinder), false);
                
                if (value != original)
                    UnityEditor.SceneView.RepaintAll();
                
                UnitySaveData.SetData(nameof(ShowPathFinder), value);
            }
        }
    }
}
