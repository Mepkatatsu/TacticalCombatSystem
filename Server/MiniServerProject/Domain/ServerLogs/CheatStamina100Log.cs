using Script.CommonLib.Responses;

namespace MiniServerProject.Domain.ServerLogs
{
    public class CheatStamina100Log
    {
        public ulong LogId { get; private set; }

        public ulong UserId { get; private set; }
        public string RequestId { get; private set; } = null!;
        public ushort AfterStamina { get; private set; }
        public DateTime DateTime { get; private set; }

        protected CheatStamina100Log() { }

        public CheatStamina100Log(ulong userId, string requestId, ushort afterStamina, DateTime dateTime)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId is required.");

            UserId = userId;
            RequestId = requestId;
            AfterStamina = afterStamina;
            DateTime = dateTime;
        }

        public CheatStamina100Response CreateResponse()
        {
            var response = new CheatStamina100Response()
            {
                AfterStamina = AfterStamina,
            };

            return response;
        }
    }
}
