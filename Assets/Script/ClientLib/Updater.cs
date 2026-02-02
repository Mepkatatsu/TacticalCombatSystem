using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Script.CommonLib;
using Script.CommonLib.Map;
using UnityEngine;
using Vector3 = Script.CommonLib.Vector3;

public class Updater : MonoBehaviour
{
    public GameObject testObj;
    
    private Entity _entity;

    private float elapsed;
    
    private BattleMapData _battleMapData;
    private BattleMapPathFinder _battleMapPathFinder;

    private GridPos _startPos;
    private GridPos _endPos;
    private readonly List<GridPos> _waypoints = new();
    
    // Start is called before the first frame update
    private void Start()
    {
        _entity = new Entity();
        
        var path = $"Assets/Data/MapData/BattleMap_Data.json";
        var json = File.ReadAllText(path);
        var battleMapData = JsonConvert.DeserializeObject<BattleMapData>(json);

        _battleMapData = battleMapData;
        _battleMapPathFinder = new BattleMapPathFinder(battleMapData);
        
        _startPos = new GridPos((int)testObj.transform.position.x, (int)testObj.transform.position.z);

        var endPositions =
            _battleMapData.battlePositions.FindAll(e => e.positionType == BattlePositionData.PositionType.EndPosition);
        
        var rand = Random.Range(0, endPositions.Count);
        
        _endPos = endPositions[rand].gridPos;
        
        _battleMapPathFinder.FindWaypoints(_startPos, _endPos, _waypoints);
    }

    // Update is called once per frame
    private void Update()
    {
        var deltaTime = Time.deltaTime;
        elapsed += deltaTime;

        const float moveSpeed = 10;

        if (_waypoints.IsEmpty())
            return;

        var nextPoint = _waypoints.First();

        if (nextPoint == default)
            return;

        var curPos = testObj.transform.position;
        var nextVec3 = new UnityEngine.Vector3(nextPoint.X, 0, nextPoint.Y);

        if (UnityEngine.Vector3.Distance(curPos, nextVec3) < 0.1f)
        {
            _waypoints.Remove(nextPoint);
            return;
        }
        
        var nextMoveVector = nextVec3 - curPos;

        var dir = nextMoveVector.normalized;
        var moveDistance = deltaTime * moveSpeed;
        
        var nextPos = curPos + dir * moveDistance;
        
        testObj.transform.position = nextPos;
        
        LogHelper.Log($"nextVec3: {nextVec3}");
        
        testObj.transform.rotation = Quaternion.Slerp(testObj.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        
        // _entity.Update(Time.deltaTime);
        
        LogHelper.Log($"pos: {_entity.GetPos()}");
    }
}
