using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using QRCoder;
using System;

namespace ProjectK.Infrastructure.Services
{
    public class MfaService : IMfaService
    {
        public string GenerateQrCodeBase64(string authenticatorUri)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }
    }
}
