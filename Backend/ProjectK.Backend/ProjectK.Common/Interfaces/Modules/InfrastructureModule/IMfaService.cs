namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule
{
    public interface IMfaService
    {
        string GenerateQrCodeBase64(string authenticatorUri);
    }
}
