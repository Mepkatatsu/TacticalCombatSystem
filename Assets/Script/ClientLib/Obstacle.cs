using System;
using System.Collections.Generic;
using Script.CommonLib;
using UnityEngine;
using Vector2 = Script.CommonLib.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Script.ClientLib
{
    public class Obstacle : MonoBehaviour
    {
        public enum ObstacleType
        {
            Square,
            // Custom   // 추후 필요할 듯, 이동 가능한 지점도 직접 찍어주는 방식을 활용해야 할 듯?
        }
        
        public ObstacleType obstacleType;

        // ObstacleType이 Square일 때만 사용
        [Range(1, BattleMapData.MaxMapSize)] public int width = 1;
        [Range(1, BattleMapData.MaxMapSize)] public int height = 1;
        
        private readonly List<GridPos> _waypoints = new();
        private readonly List<GridPos> _blockedPoints = new();
        
        private Vector3 _waypointsSavedPosition;
        private int _waypointsSavedWidth;
        private int _waypointsSavedHeight;
        private ObstacleType _waypointsSavedObstacleType;
        
        private Vector3 _blockedPointsSavedPosition;
        private int _blockedPointsSavedWidth;
        private int _blockedPointsSavedHeight;
        private ObstacleType _blockedPointsSavedObstacleType;

        public List<GridPos> GetWaypoints()
        {
            if (obstacleType != ObstacleType.Square)
                return _waypoints;

            if (_waypoints.IsNotEmpty() && _waypointsSavedPosition == transform.position && _waypointsSavedWidth == width &&
                _waypointsSavedHeight == height && _waypointsSavedObstacleType == obstacleType)
                return _waypoints;
            
            _waypoints.Clear();
            
            var center = transform.position;
            
            // waypoint는 충돌 지점과 겹치면 안 되기 때문에 계산할 때 0.5씩 여유를 주고 계산하도록 한다.
            
            // 값을 뺄 때는 값이 작아지는 방향으로 소수점을 맞춰줘야 한다
            var left = (int)MathF.Floor(center.x - width / 2f - 0.5f);
            var bottom = (int)MathF.Floor(center.z - height / 2f - 0.5f);
            
            // 값을 더할 때는 값이 커지는 방향으로 소수점을 맞춰줘야 한다
            var right = (int)MathF.Ceiling(center.x + width / 2f + 0.5f);
            var top = (int)MathF.Ceiling(center.z + height / 2f + 0.5f);
            
            _waypoints.Add(new GridPos(left, top));
            _waypoints.Add(new GridPos(right, top));
            _waypoints.Add(new GridPos(left, bottom));
            _waypoints.Add(new GridPos(right, bottom));
            
            _waypointsSavedPosition = transform.position;
            _waypointsSavedWidth = width;
            _waypointsSavedHeight = height;
            _waypointsSavedObstacleType = obstacleType;
            
            return _waypoints;
        }

        public List<GridPos> GetBlockedPoints()
        {
            if (obstacleType != ObstacleType.Square)
                return _blockedPoints;

            if (_blockedPoints.IsNotEmpty() && _waypoints.IsNotEmpty() && _blockedPointsSavedPosition == transform.position && _blockedPointsSavedWidth == width 
                && _blockedPointsSavedHeight == height && _blockedPointsSavedObstacleType == obstacleType)
                return _blockedPoints;
            
            _blockedPoints.Clear();
            
            var center = transform.position;
            
            // waypoint는 충돌 지점과 겹치면 안 되기 때문에 계산할 때 0.5씩 여유를 주고 계산하도록 한다.
            
            // waypoint의 안쪽을 채워줌
            var left = (int)MathF.Floor(center.x - width / 2f - 0.5f) + 1;
            var bottom = (int)MathF.Floor(center.z - height / 2f - 0.5f) + 1;
            var right = (int)MathF.Ceiling(center.x + width / 2f + 0.5f) - 1;
            var top = (int)MathF.Ceiling(center.z + height / 2f + 0.5f) - 1;

            for (var i = left; i <= right; i++)
            {
                for (var j = bottom; j <= top; j++)
                {
                    _blockedPoints.Add(new GridPos(i, j));
                }
            }
            
            _blockedPointsSavedPosition = transform.position;
            _blockedPointsSavedWidth = width;
            _blockedPointsSavedHeight = height;
            _blockedPointsSavedObstacleType = obstacleType;
            
            return _blockedPoints;
        }

        public ObstacleData ToObstacleData()
        {
            var obstacleData = new ObstacleData
            {
                position = new Vector2(transform.position.x, transform.position.z),
                waypoints = GetWaypoints(),
                blockedPoints = GetBlockedPoints(),
            };

            return obstacleData;
        }
    }
}
