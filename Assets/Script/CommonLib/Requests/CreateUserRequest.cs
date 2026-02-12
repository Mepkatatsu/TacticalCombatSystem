namespace Script.CommonLib.Requests
{
    public sealed class CreateUserRequest
    {
        public string AccountId { get; }
        public string Nickname { get; }

        public CreateUserRequest(string accountId, string nickname)
        {
            AccountId = accountId;
            Nickname = nickname;
        }
    }
}
