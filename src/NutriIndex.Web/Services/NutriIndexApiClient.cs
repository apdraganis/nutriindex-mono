using System.Net.Http.Json;
using NutriIndex.Core.Models;

namespace NutriIndex.Web.Services;

public class NutriIndexApiClient
{
    private readonly HttpClient _httpClient;

    public NutriIndexApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductInfo?> GetProductAsync(string barcode)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProductInfo>($"api/products/{Uri.EscapeDataString(barcode)}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Indices?> CalculateAsync(CalculateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/calculate", request);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<Indices>();
    }
}
