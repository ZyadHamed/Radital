namespace RadiologistAppCore.Identities
{
    public static class SystemIdentities
    {
        public static readonly Guid HospitalRequestsApi =
            Guid.Parse("00000000-0000-0000-0000-000000000001");

        public static bool IsSystem(Guid id) => id == HospitalRequestsApi;
    }
}
