using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Script.CommonLib;
using Script.CommonLib.Requests;
using Script.CommonLib.Responses;
using Script.CommonLib.Tables;

namespace Script.ClientLib.Network.App
{
    public class TestClientApp
    {
        private readonly ClientContext _ctx = new();
        
        public async Task<bool> ConnectToServer(string url, string accountId)
        {
            _ctx.BaseUrl = url;
            _ctx.BuildApi();
            var response = await _ctx.Api.GetAsync<UserResponse>($"/users/{accountId}", true);

            if (response == null)
            {
                var createRequest = new CreateUserRequest(accountId, accountId);
                response = await _ctx.Api.PostAsync<CreateUserRequest, UserResponse>($"/users", createRequest);
            }

            if (response == null)
            {
                LogHelper.Error("Connect to server failed: CreateUser Failed");
                return false;
            }
            
            _ctx.InitByUserResponse(accountId, response);
            return true;
        }

        public async Task<bool> RequestEnterStage(string stageName)
        {
            if (!_ctx.IsInitialized)
            {
                LogHelper.Error("RequestEnterStage: ClientContext is not Initialized");
                return false;
            }

            if (_ctx.CurrentStageId == stageName)
                return true;

            var stageTable = TableHolder.GetTable<StageTable>();
            var stageData = stageTable.Get(stageName);

            if (stageData == null)
            {
                LogHelper.Error("RequestEnterStage: StageData not found");
                return false;
            }

            if (_ctx.Stamina < stageData.NeedStamina)
            {
                var cheatRequest = new CheatStamina100Request(_ctx.UserId, _ctx.GetRequestId());
                var cheatResponse = await _ctx.Api.PostAsync<CheatStamina100Request, CheatStamina100Response>($"/cheat/{_ctx.UserId}/stamina100", cheatRequest);

                if (cheatResponse == null)
                {
                    LogHelper.Error("RequestEnterStage: CheatStamina100Request Failed");
                    return false;
                }
                
                _ctx.SetStamina(cheatResponse.AfterStamina);
            }

            if (_ctx.Stamina < stageData.NeedStamina)
            {
                LogHelper.Error("RequestEnterStage: not enough Stamina");
                return false;
            }
            
            var enterRequest = new EnterStageRequest(_ctx.UserId, _ctx.GetRequestId());
            var enterResponse = await _ctx.Api.PostAsync<EnterStageRequest, EnterStageResponse>($"/stages/{stageName}/enter", enterRequest);

            if (enterResponse == null)
            {
                LogHelper.Error("RequestEnterStage: EnterStageRequest Failed");
                return false;
            }

            return true;
        }
        
        public async Task<List<Tuple<uint, float>>> RequestVerifyStageBattle(List<float> updateIntervals)
        {
            // TODO: 프레임과 전투 시간에 따라 패킷 크기가 커질 수 있으니 이에 대한 처리를 해야 함. 60FPS 기준 3분까지는 괜찮을 듯 (+float 대신 ms 기준으로 Update를 해서 안정성을 보장하면 좋을 듯)
            
            if (!_ctx.IsInitialized)
            {
                LogHelper.Error("RequestEnterStage: ClientContext is not Initialized");
                return null;
            }

            if (_ctx.CurrentStageId == null)
            {
                LogHelper.Error("RequestEnterStage: CurrentStageId is null");
                return null;
            }

            var request = new VerifyStageBattleRequest(_ctx.UserId, _ctx.GetRequestId(), updateIntervals);
            var response = await _ctx.Api.PostAsync<VerifyStageBattleRequest, VerifyStageBattleResponse>($"/stages/{_ctx.CurrentStageId}/verify-battle", request);

            if (response == null)
            {
                LogHelper.Error("RequestEnterStage: VerifyStageBattleRequest Failed");
                return null;
            }
            
            LogHelper.Log($"RequestVerifyStageBattle Winner: {response.Winner}");
            
            return response.AliveEntities;
        }
    }
}
