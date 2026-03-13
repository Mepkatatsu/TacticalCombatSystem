using Script.CommonLib;
using Script.CommonLib.Map;

namespace MiniServerProject.Domain.Battle;

public class ServerBattleMapSimulator : IBattleMapEventHandler
{
    public TeamFlag Winner { get; private set; }
    public bool IsBattleEnded { get; private set; }

    private BattleMapSimulator _battleMapSimulator;
    private BattleMapData _battleMapData;
    private List<ushort> _updateIntervals;
    
    private List<Entity> _entities = new();

    public uint TotalElapsedMs { get; private set; }
    public uint TotalElapsedFrames { get; private set; }

    public ServerBattleMapSimulator(BattleMapData battleMapData, List<ushort> updateIntervals)
    {
        _battleMapSimulator = new BattleMapSimulator(this, battleMapData);
        _battleMapData = battleMapData;
        _updateIntervals = updateIntervals;
        
        _battleMapSimulator.Init();
    }

    public void Simulate()
    {
        if (IsBattleEnded)
            return;

        foreach (var updateInterval in _updateIntervals)
        {
            _battleMapSimulator.Update(updateInterval);
        }
    }

    public Dictionary<uint, IEntityContext> GetAliveEntitiesDictionary()
    {
        return _battleMapSimulator.GetAliveEntitiesDictionary();
    }
    
    public void OnEntityAdded(uint entityId, Entity entity)
    {
        _entities.Add(entity);
    }

    public void OnEntityPositionChanged(uint entityId, FixedPos pos)
    {
        
    }

    public void OnEntityDirectionChanged(uint entityId, FixedDir dir)
    {
        
    }

    public void OnEntityStartMove(uint entityId)
    {
        
    }

    public void OnEntityStopMove(uint entityId)
    {
        
    }

    public void OnEntityStartAttack(uint attackerId, uint targetId)
    {
        
    }

    public void OnEntityGetDamage(uint entityId, uint damage)
    {
        
    }

    public void OnEntityRetired(uint entityId)
    {
        
    }

    public void OnProjectileAdded(ulong projectileId, Projectile projectile)
    {
        
    }

    public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos)
    {
        
    }

    public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir)
    {
        
    }

    public void OnProjectileTriggered(ulong projectileId)
    {
        
    }

    public void OnBattleEnd(TeamFlag winner)
    {
        IsBattleEnded = true;
        Winner = winner;
    }

    public void OnBattleMapUpdated(ushort elapsedMs)
    {
        TotalElapsedMs += elapsedMs;
        ++TotalElapsedFrames;
    }
}