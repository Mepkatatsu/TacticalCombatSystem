using Script.CommonLib;
using Script.CommonLib.Map;

namespace MiniServerProject.Domain.Battle;

public class ServerBattleMapSimulator : IBattleMapEventHandler
{
    public TeamFlag Winner { get; private set; }
    public bool IsBattleEnded { get; private set; }

    private BattleMapSimulator _battleMapSimulator;
    private BattleMapData _battleMapData;
    private List<float> _updateIntervals;
    
    private List<Entity> _entities = new();

    public ServerBattleMapSimulator(BattleMapData battleMapData, List<float> updateIntervals)
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

    public List<IEntityContext> GetAliveEntities()
    {
        var aliveEntities = new List<IEntityContext>();

        foreach (var entity in _entities)
        {
            if (entity.IsAlive())
                aliveEntities.Add(entity);
        }

        return aliveEntities;
    }
    
    public void OnEntityAdded(uint entityId, Entity entity)
    {
        _entities.Add(entity);
    }

    public void OnEntityPositionChanged(uint entityId, Vec3 pos)
    {
        
    }

    public void OnEntityDirectionChanged(uint entityId, Vec3 pos)
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

    public void OnEntityGetDamage(uint entityId, float damage)
    {
        
    }

    public void OnEntityRetired(uint entityId)
    {
        
    }

    public void OnProjectileAdded(ulong projectileId, Projectile projectile)
    {
        
    }

    public void OnProjectilePositionChanged(ulong projectileId, Vec3 pos)
    {
        
    }

    public void OnProjectileDirectionChanged(ulong projectileId, Vec3 dir)
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

    public void OnBattleMapUpdated(float elapsedTime)
    {
        
    }
}