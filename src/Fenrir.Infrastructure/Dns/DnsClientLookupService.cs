using DnsClient;
using DnsClient.Protocol;
using Fenrir.Application.Abstractions;

namespace Fenrir.Infrastructure.Dns;

public sealed class DnsClientLookupService(ILookupClient lookupClient) : IDnsLookupService
{
    public async Task<IReadOnlyList<string>> GetARecordsAsync(string domain, CancellationToken cancellationToken)
    {
        var answers = await QueryAnswersSafeAsync(domain, QueryType.A, cancellationToken);
        return answers.ARecords().Select(record => record.Address.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<IReadOnlyList<string>> GetAaaaRecordsAsync(string domain, CancellationToken cancellationToken)
    {
        var answers = await QueryAnswersSafeAsync(domain, QueryType.AAAA, cancellationToken);
        return answers.AaaaRecords().Select(record => record.Address.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<IReadOnlyList<string>> GetMxRecordsAsync(string domain, CancellationToken cancellationToken)
    {
        var answers = await QueryAnswersSafeAsync(domain, QueryType.MX, cancellationToken);
        return answers.MxRecords()
            .Select(record => $"{record.Preference} {record.Exchange.Value.TrimEnd('.')}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<string>> GetTxtRecordsAsync(string domain, CancellationToken cancellationToken)
    {
        var answers = await QueryAnswersSafeAsync(domain, QueryType.TXT, cancellationToken);
        return answers.TxtRecords()
            .Select(record => string.Join("", record.Text))
            .Where(record => !string.IsNullOrWhiteSpace(record))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<string>> GetNameServersAsync(string domain, CancellationToken cancellationToken)
    {
        var answers = await QueryAnswersSafeAsync(domain, QueryType.NS, cancellationToken);
        return answers.NsRecords()
            .Select(record => record.NSDName.Value.TrimEnd('.'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<string>> GetCaaRecordsAsync(string domain, CancellationToken cancellationToken)
    {
        var answers = await QueryAnswersSafeAsync(domain, QueryType.CAA, cancellationToken);
        return answers
            .OfType<CaaRecord>()
            .Select(record => record.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<bool> HasDnsSecAsync(string domain, CancellationToken cancellationToken)
    {
        var dsResult = await QueryAnswersSafeAsync(domain, QueryType.DS, cancellationToken);
        if (dsResult.Count > 0)
        {
            return true;
        }

        var dnsKeyResult = await QueryAnswersSafeAsync(domain, QueryType.DNSKEY, cancellationToken);
        return dnsKeyResult.Count > 0;
    }

    private async Task<IReadOnlyList<DnsResourceRecord>> QueryAnswersSafeAsync(string domain, QueryType queryType, CancellationToken cancellationToken)
    {
        try
        {
            var response = await lookupClient.QueryAsync(domain, queryType, cancellationToken: cancellationToken);
            return response.Answers.ToArray();
        }
        catch (DnsResponseException)
        {
            return [];
        }
        catch (TimeoutException)
        {
            return [];
        }
    }
}
