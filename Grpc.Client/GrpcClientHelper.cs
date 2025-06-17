using Grpc.Net.Client;
using System.Net.Http;

public static class GrpcClientHelper
{
    public static GrpcChannel CreateChannel(string address)
    {
        var httpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = httpHandler });
    }
}
