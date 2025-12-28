using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure;

public class ApiHelper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiHelper(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> PostAsync<T>(string url, object body)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<T?> PostFormDataAsync<T>(string url, Dictionary<string, string> formData)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(formData);
        var response = await httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<T?> PutAsync<T>(string url, object body)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PutAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<T?> DeleteAsync<T>(string url)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }
}