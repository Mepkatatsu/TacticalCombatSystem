using Microsoft.EntityFrameworkCore;
using MiniServerProject.Domain.ServerLogs;
using MiniServerProject.Infrastructure;
using MiniServerProject.Infrastructure.Persistence;
using Script.CommonLib.Responses;

namespace MiniServerProject.Application.Cheats
{
    public class CheatService : ICheatService
    {
        private readonly GameDbContext _db;
        private readonly IIdempotencyCache _idemCache;
        private readonly ILogger<CheatService> _logger;

        public CheatService(GameDbContext db, IIdempotencyCache idemCache, ILogger<CheatService> logger)
        {
            _db = db;
            _idemCache = idemCache;
            _logger = logger;
        }
        
        public async Task<CheatStamina100Response> CheatStamina100(ulong userId, string requestId, CancellationToken ct)
        {
            // 1) Redis 캐시 조회
            var cacheKey = IdempotencyKeyFactory.CheatStamina100(userId, requestId);
            var response = await _idemCache.GetAsync<CheatStamina100Response>(cacheKey);
            if (response != null)
            {
                _logger.LogInformation("Idempotent response served from Redis. userId={userId}, requestId={request.RequestId}, cacheKey={cacheKey}",
                    userId, requestId, cacheKey);
                return response;
            }

            // 2) DB log 선조회
            var log = await FindCheatStamina100LogAsync(userId, requestId, ct);
            if (log != null)
            {
                response = log.CreateResponse();
                await _idemCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
                return response;
            }
            
            var now = DateTime.UtcNow;

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct)
                        ?? throw new DomainException(ErrorType.UserNotFound);
            
            user.AddStamina(100, now);

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var affected = await _db.Users
                                    .Where(u => u.UserId == userId)
                                    .ExecuteUpdateAsync(setters =>
                                        setters.SetProperty(u => u.Stamina, user.Stamina)
                                    , ct);

            if (affected == 0)
                throw new DomainException(ErrorType.UserNotFound);

            try
            {
                // 응답 멱등성 보장을 위해 로그 INSERT
                log = new CheatStamina100Log(user.UserId, requestId, user.Stamina, now);
                _db.CheatStamina100Logs.Add(log);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                log = await FindCheatStamina100LogAsync(userId, requestId, ct);
                if (log == null)
                {
                    _logger.LogError(
                        ex,
                        "Idempotency log missing after unique violation. userId={UserId} requestId={RequestId}",
                        userId, requestId);

                    throw new DomainException(ErrorType.IdempotencyMissingAfterUniqueViolation);
                }

                response = log.CreateResponse();
                await _idemCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
                return response;
            }

            response = log.CreateResponse();
            await _idemCache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return response;
        }
        
        private async Task<CheatStamina100Log?> FindCheatStamina100LogAsync(ulong userId, string requestId, CancellationToken ct)
        {
            return await _db.CheatStamina100Logs
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.RequestId == requestId)
                .FirstOrDefaultAsync(ct);
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is MySqlConnector.MySqlException mysqlException
                && mysqlException.ErrorCode == MySqlConnector.MySqlErrorCode.DuplicateKeyEntry;
        }
    }
}
