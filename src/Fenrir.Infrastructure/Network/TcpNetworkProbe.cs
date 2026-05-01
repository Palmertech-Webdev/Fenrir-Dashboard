using System.Net.Sockets;
using System.Text;
using Fenrir.Application.Abstractions;

namespace Fenrir.Infrastructure.Network;

public sealed class TcpNetworkProbe : INetworkProbe
{
    public async Task<PortProbeResult> ProbeAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, linkedCts.Token);
            var banner = await TryReadBannerAsync(client, linkedCts.Token);
            return new PortProbeResult(true, banner);
        }
        catch (OperationCanceledException)
        {
            return new PortProbeResult(false, null);
        }
        catch (SocketException)
        {
            return new PortProbeResult(false, null);
        }
        catch (IOException)
        {
            return new PortProbeResult(false, null);
        }
    }

    private static async Task<string?> TryReadBannerAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            readCts.CancelAfter(TimeSpan.FromMilliseconds(500));
            var stream = client.GetStream();
            if (!stream.CanRead)
            {
                return null;
            }

            var buffer = new byte[256];
            var bytesRead = await stream.ReadAsync(buffer, readCts.Token);
            if (bytesRead <= 0)
            {
                return null;
            }

            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        }
        catch
        {
            return null;
        }
    }
}
