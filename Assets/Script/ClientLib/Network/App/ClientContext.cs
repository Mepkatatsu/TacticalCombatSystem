using System;
using Script.ClientLib.Network.Api;
using Script.CommonLib.Responses;

namespace Script.ClientLib.Network.App
{
    public sealed class ClientContext
    {
        public AppState State { get; set; } = AppState.Boot;

        public string BaseUrl { get; set; } = "http://localhost:5099";
        public string AccountId { get; private set; }
        public ulong UserId { get; private set; }
        public string Nickname { get; private set; }
        public ushort Level { get; private set; }
        public ushort Stamina { get; private set; }
        public ulong Gold { get; private set; }
        public ulong Exp { get; private set; }
        public string CurrentStageId { get; private set; }

        public ApiClient Api { get; private set; } = null!;

        public string GetRequestId() => Guid.NewGuid().ToString();
        public bool IsInitialized { get; private set; }

        public void InitByUserResponse(string accountId, UserResponse userResponse)
        {
            AccountId = accountId;
            UserId = userResponse.UserId;
            Nickname = userResponse.Nickname;
            Level = userResponse.Level;
            Stamina = userResponse.Stamina;
            Gold = userResponse.Gold;
            Exp = userResponse.Exp;
            CurrentStageId = userResponse.CurrentStageId;
            
            IsInitialized = true;
        }

        public void SetAccountId(string accountId)
        {
            AccountId = accountId;
        }

        public void ClearAccountId()
        {
            AccountId = null;
        }

        public void SetStamina(ushort stamina)
        {
            Stamina = stamina;
        }

        public void SetGold(ulong gold)
        {
            Gold = gold;
        }

        public void SetExp(ulong exp)
        {
            Exp = exp;
        }

        public void SetCurrentStageId(string stageId)
        {
            CurrentStageId = stageId;
        }

        public void ClearCurrentStageId()
        {
            CurrentStageId = null;
        }

        public void BuildApi()
        {
            Api = new ApiClient(BaseUrl);
        }

        public string GetStageName()
        {
            if (string.IsNullOrWhiteSpace(CurrentStageId))
                return "없음";

            return CurrentStageId;
        }
    }
}
