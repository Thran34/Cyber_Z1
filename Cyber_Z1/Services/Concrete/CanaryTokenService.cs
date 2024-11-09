using Cyber_Z1.Services.Abstract;

namespace Cyber_Z1.Services.Concrete;


public class CanaryTokenService : ICanaryTokenService
{
    private readonly HttpClient _httpClient;
    private readonly string _canaryTokenUrl;

    public CanaryTokenService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _canaryTokenUrl = configuration["CanaryTokens:HTTPTokenURL"];
    }

    public async Task TriggerTokenAsync()
    {
        await _httpClient.GetAsync(_canaryTokenUrl);
    }
}