using System.Net.Http.Json;
using System.Text.Json;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri(ApiConfig.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    // Customers
    public Task<List<Customer>?> GetCustomersAsync()
        => _http.GetFromJsonAsync<List<Customer>>("api/customers", JsonOptions);

    public async Task<Customer?> CreateCustomerAsync(Customer customer)
    {
        var response = await _http.PostAsJsonAsync("api/customers", customer);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Customer>(JsonOptions);
    }

    // Job cards
    public Task<List<JobCard>?> GetJobCardsAsync()
        => _http.GetFromJsonAsync<List<JobCard>>("api/jobcards", JsonOptions);

    public Task<JobCard?> GetJobCardAsync(int id)
        => _http.GetFromJsonAsync<JobCard>($"api/jobcards/{id}", JsonOptions);

    public async Task<JobCard?> CreateJobCardAsync(JobCard jobCard)
    {
        var response = await _http.PostAsJsonAsync("api/jobcards", jobCard);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JobCard>(JsonOptions);
    }

    public async Task UpdateJobCardAsync(JobCard jobCard)
    {
        var response = await _http.PutAsJsonAsync($"api/jobcards/{jobCard.Id}", jobCard);
        response.EnsureSuccessStatusCode();
    }

    // Invoices
    public Task<List<Invoice>?> GetInvoicesAsync()
        => _http.GetFromJsonAsync<List<Invoice>>("api/invoices", JsonOptions);

    public async Task<Invoice?> CreateInvoiceFromJobCardAsync(int jobCardId)
    {
        var response = await _http.PostAsync($"api/invoices/from-jobcard/{jobCardId}", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<Invoice>(JsonOptions);
    }

    public async Task SetInvoiceStatusAsync(int invoiceId, InvoiceStatus status)
    {
        var response = await _http.PostAsync($"api/invoices/{invoiceId}/status/{status}", null);
        response.EnsureSuccessStatusCode();
    }
}
