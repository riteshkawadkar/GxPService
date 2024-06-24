namespace GxPService
{
    public static class GlobalConstants
    {
        public const string RegistryGxPServiceKey = @"SOFTWARE\VShield\GxPService";
        public const string RegistryAgentsTrustedPathKey = @"Software\VShield\GxPService\Agents\";

        public const string DatabasePath = @"Origami IT Lab\VShield\GxPService";
        public const string DatabaseFileName = "service.gxp";

        public const string AHKPath = @"Origami IT Lab\VShield\GxPService\AHK";
        public const string AHKFileName = "configuration.enc";

        public const string PolicyFileName = "policies.enc";

        public const string AgentsExecutablePath = @"C:\ProgramData\Origami IT Lab\VShield\GxPService\Agents";
        public const string AgentsRunnerCode = "a13e07c188ba1fd889773096e0bfeccf";

        public const string KprocHash = "058b7da30afd51c1e1869ddc08b9dc4e1e18e211fc64776cce82272d3790bd1f";
        public const string MprocHash = "2862890e53aaa999faf3b79747f2f800bcb44930f9452b97b721c51295c6066d";
        public const string QatHash = "cd0b3e74cc9d687c40bd616cff61fcef62aa7e560584613a1feadb9f5e471ab8";
        public const string RibbonHash = "1675dc634525111c022d9ee165b099516dfe4f39f8038fe0e5b00324e6f7210c";
        public const string Ribbon7Hash = "497d0ab6d5de50a3e023d5c69d634b5946c7253d37fab298728b1a5a41d199d0";
        public const string Ribbon11Hash = "e905bb6cd0fed0efaf1cf9f5fb25efa63a1223442dede89725e157109b585de8";
        public const string RnsHash = "7a5ed6236c0c44e6b910c21e2ab349d8d47ca7d91aa573beab6533377f15faae";
        public const string StartShellHash = "ccbe4e26a70b9b5cb460baebc6761edce6786f0889f4947f45a2defdd2f90959";
    }
}
