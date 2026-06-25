using ScannerShared;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ScannerClientWpf.Services;

public sealed class NetworkScannerDiscoveryService
{
    private const int MaxHostsPerNetwork = 254;
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMilliseconds(900)
    };

    public async Task<IReadOnlyList<DiscoveredScannerServer>> DiscoverAsync(int port, CancellationToken cancellationToken)
    {
        var addresses = GetCandidateAddresses().Distinct().OrderBy(x => x.ToString()).ToArray();
        var found = new ConcurrentBag<DiscoveredScannerServer>();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 64,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(addresses, options, async (address, token) =>
        {
            var url = $"http://{address}:{port}";
            try
            {
                var status = await _http.GetFromJsonAsync<StatusDto>($"{url}/api/status", token);
                if (status?.Online == true)
                    found.Add(new DiscoveredScannerServer(address.ToString(), url, status.ServerName));
            }
            catch
            {
                // Offline hosts are expected while scanning the local network.
            }
        });

        return found.OrderBy(x => x.IpAddress).ToArray();
    }

    private static IEnumerable<IPAddress> GetCandidateAddresses()
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var unicast in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork || unicast.IPv4Mask is null)
                    continue;

                foreach (var address in ExpandNetwork(unicast.Address, unicast.IPv4Mask))
                    yield return address;
            }
        }

        yield return IPAddress.Loopback;
    }

    private static IEnumerable<IPAddress> ExpandNetwork(IPAddress address, IPAddress mask)
    {
        var ip = ToUInt32(address);
        var subnetMask = ToUInt32(mask);
        var network = ip & subnetMask;
        var broadcast = network | ~subnetMask;
        var hostCount = broadcast > network ? broadcast - network - 1 : 0;

        if (hostCount is 0 or > MaxHostsPerNetwork)
        {
            var classCNetwork = ip & 0xFFFFFF00;
            for (uint i = 1; i <= MaxHostsPerNetwork; i++)
                yield return FromUInt32(classCNetwork + i);

            yield break;
        }

        for (var candidate = network + 1; candidate < broadcast; candidate++)
            yield return FromUInt32(candidate);
    }

    private static uint ToUInt32(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }

    private static IPAddress FromUInt32(uint value)
    {
        return new IPAddress(new[]
        {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        });
    }
}

public sealed record DiscoveredScannerServer(string IpAddress, string Url, string ServerName)
{
    public string DisplayName => $"{IpAddress} - {ServerName}";
}
