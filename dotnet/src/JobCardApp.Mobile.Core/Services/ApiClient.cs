using System.Net.Http.Headers;
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

    /// <summary>Attaches (or clears, if null) the bearer token used for every subsequent request.</summary>
    public void AttachToken(string? token)
        => _http.DefaultRequestHeaders.Authorization =
            token is null ? null : new AuthenticationHeaderValue("Bearer", token);

    // Auth
    public async Task<AuthResponse?> LoginAsync(string username, string password, bool rememberMe)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login",
            new LoginRequest { Username = username, Password = password, RememberMe = rememberMe });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
    }

    // Settings
    public Task<BillingSettings?> GetBillingSettingsAsync()
        => _http.GetFromJsonAsync<BillingSettings>("api/settings/billing", JsonOptions);

    // Companies
    public Task<List<Company>?> GetCompaniesAsync()
        => _http.GetFromJsonAsync<List<Company>>("api/companies", JsonOptions);

    public Task<Company?> GetCompanyAsync(int id)
        => _http.GetFromJsonAsync<Company>($"api/companies/{id}", JsonOptions);

    public async Task<Company?> CreateCompanyAsync(Company company)
    {
        var response = await _http.PostAsJsonAsync("api/companies", company);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Company>(JsonOptions);
    }

    public async Task UpdateCompanyAsync(Company company)
    {
        var response = await _http.PutAsJsonAsync($"api/companies/{company.Id}", company);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    public async Task DeleteCompanyAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/companies/{id}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    // Customers
    public Task<List<Customer>?> GetCustomersAsync(string? search = null)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "api/customers"
            : $"api/customers?search={Uri.EscapeDataString(search)}";
        return _http.GetFromJsonAsync<List<Customer>>(url, JsonOptions);
    }

    public Task<Customer?> GetCustomerAsync(int id)
        => _http.GetFromJsonAsync<Customer>($"api/customers/{id}", JsonOptions);

    public async Task<Customer?> CreateCustomerAsync(Customer customer)
    {
        var response = await _http.PostAsJsonAsync("api/customers", customer);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Customer>(JsonOptions);
    }

    public async Task UpdateCustomerAsync(Customer customer)
    {
        var response = await _http.PutAsJsonAsync($"api/customers/{customer.Id}", customer);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteCustomerAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/customers/{id}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Previous pricing for this customer. <paramref name="kind"/> scopes the
    /// result to the same kind of work (the server matches the "[Kind]" prefix
    /// on the invoice line) so a labour price never comes back as a suggestion
    /// for a part; the server also limits it to the last six months.
    /// </summary>
    public Task<List<PricingHistoryEntry>?> GetPricingHistoryAsync(int customerId, string? search = null, LineKind? kind = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (kind.HasValue) query.Add($"kind={kind.Value}");

        var url = $"api/customers/{customerId}/pricing-history" + (query.Count > 0 ? $"?{string.Join("&", query)}" : "");
        return _http.GetFromJsonAsync<List<PricingHistoryEntry>>(url, JsonOptions);
    }

    public Task<CustomerStatement?> GetCustomerStatementAsync(int customerId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = new List<string>();
        if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        var url = $"api/customers/{customerId}/statement" + (query.Count > 0 ? $"?{string.Join("&", query)}" : "");
        return _http.GetFromJsonAsync<CustomerStatement>(url, JsonOptions);
    }

    // Customer items
    public Task<List<CustomerItem>?> GetCustomerItemsAsync(int customerId)
        => _http.GetFromJsonAsync<List<CustomerItem>>($"api/customers/{customerId}/items", JsonOptions);

    public async Task<CustomerItem?> CreateCustomerItemAsync(int customerId, string name, string? category)
    {
        var response = await _http.PostAsJsonAsync($"api/customers/{customerId}/items", new CustomerItem { Name = name, Category = category });
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<CustomerItem>(JsonOptions);
    }

    public Task<List<CustomerItemHistoryEntry>?> GetCustomerItemHistoryAsync(int itemId)
        => _http.GetFromJsonAsync<List<CustomerItemHistoryEntry>>($"api/customer-items/{itemId}/history", JsonOptions);

    public Task<CustomerItem?> GetCustomerItemAsync(int itemId)
        => _http.GetFromJsonAsync<CustomerItem>($"api/customer-items/{itemId}", JsonOptions);

    public async Task<CustomerItem?> UpdateCustomerItemAsync(int itemId, string name, string? category)
    {
        var response = await _http.PutAsJsonAsync($"api/customer-items/{itemId}", new CustomerItem { Name = name, Category = category });
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<CustomerItem>(JsonOptions);
    }

    public async Task DeleteCustomerItemAsync(int itemId)
    {
        var response = await _http.DeleteAsync($"api/customer-items/{itemId}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    // Customer emails
    public Task<List<CustomerEmail>?> GetCustomerEmailsAsync(int customerId)
        => _http.GetFromJsonAsync<List<CustomerEmail>>($"api/customers/{customerId}/emails", JsonOptions);

    public async Task<CustomerEmail?> AddCustomerEmailAsync(int customerId, string email)
    {
        var response = await _http.PostAsJsonAsync($"api/customers/{customerId}/emails", new CustomerEmail { Email = email });
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<CustomerEmail>(JsonOptions);
    }

    public async Task DeleteCustomerEmailAsync(int emailId)
    {
        var response = await _http.DeleteAsync($"api/customer-emails/{emailId}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    // Customer sites
    public Task<List<CustomerSite>?> GetCustomerSitesAsync(int customerId)
        => _http.GetFromJsonAsync<List<CustomerSite>>($"api/customers/{customerId}/sites", JsonOptions);

    public async Task<CustomerSite?> AddCustomerSiteAsync(int customerId, string name, string address)
    {
        var response = await _http.PostAsJsonAsync($"api/customers/{customerId}/sites", new CustomerSite { Name = name, Address = address });
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<CustomerSite>(JsonOptions);
    }

    public async Task DeleteCustomerSiteAsync(int siteId)
    {
        var response = await _http.DeleteAsync($"api/customer-sites/{siteId}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    // Job cards
    public Task<List<JobCard>?> GetJobCardsAsync(JobCardStatus? status = null)
    {
        var url = status.HasValue ? $"api/jobcards?status={status}" : "api/jobcards";
        return _http.GetFromJsonAsync<List<JobCard>>(url, JsonOptions);
    }

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

    // Status is server-owned — these are the only ways to move a job card
    // between statuses; a plain update no longer accepts a Status change.
    public async Task<JobCard?> CompleteJobCardAsync(int id)
    {
        var response = await _http.PostAsync($"api/jobcards/{id}/complete", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JobCard>(JsonOptions);
    }

    public async Task<JobCard?> CancelJobCardAsync(int id)
    {
        var response = await _http.PostAsync($"api/jobcards/{id}/cancel", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JobCard>(JsonOptions);
    }

    // Invoices
    public Task<List<Invoice>?> GetInvoicesAsync(InvoiceStatus? status = null)
    {
        var url = status.HasValue ? $"api/invoices?status={status}" : "api/invoices";
        return _http.GetFromJsonAsync<List<Invoice>>(url, JsonOptions);
    }

    public Task<Invoice?> GetInvoiceAsync(int id)
        => _http.GetFromJsonAsync<Invoice>($"api/invoices/{id}", JsonOptions);

    public async Task<Invoice?> CreateInvoiceFromJobCardAsync(int jobCardId)
    {
        var response = await _http.PostAsync($"api/invoices/from-jobcard/{jobCardId}", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<Invoice>(JsonOptions);
    }

    public async Task<Invoice?> UpdateInvoiceLinesAsync(Invoice invoice)
    {
        var response = await _http.PutAsJsonAsync($"api/invoices/{invoice.Id}", invoice);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Invoice>(JsonOptions);
    }

    public async Task SetInvoiceStatusAsync(int invoiceId, InvoiceStatus status)
    {
        var response = await _http.PostAsync($"api/invoices/{invoiceId}/status/{status}", null);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Real send: renders a PDF and emails it to the customer on file, Draft-only.</summary>
    public async Task<Invoice?> SendInvoiceAsync(int invoiceId)
    {
        var response = await _http.PostAsync($"api/invoices/{invoiceId}/send", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Invoice>(JsonOptions);
    }

    /// <summary>Reverts a Sent/Overdue invoice back to Draft so its lines become editable again.</summary>
    public async Task<Invoice?> ReviseInvoiceAsync(int invoiceId)
    {
        var response = await _http.PostAsync($"api/invoices/{invoiceId}/revise", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Invoice>(JsonOptions);
    }

    // Quotes
    public Task<List<Quote>?> GetQuotesAsync(QuoteStatus? status = null)
    {
        var url = status.HasValue ? $"api/quotes?status={status}" : "api/quotes";
        return _http.GetFromJsonAsync<List<Quote>>(url, JsonOptions);
    }

    public Task<Quote?> GetQuoteAsync(int id)
        => _http.GetFromJsonAsync<Quote>($"api/quotes/{id}", JsonOptions);

    public async Task<Quote?> CreateQuoteFromJobCardAsync(int jobCardId)
    {
        var response = await _http.PostAsync($"api/quotes/from-jobcard/{jobCardId}", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Quote>(JsonOptions);
    }

    public async Task<Quote?> UpdateQuoteLinesAsync(Quote quote)
    {
        var response = await _http.PutAsJsonAsync($"api/quotes/{quote.Id}", quote);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Quote>(JsonOptions);
    }

    public async Task SetQuoteStatusAsync(int quoteId, QuoteStatus status)
    {
        var response = await _http.PostAsync($"api/quotes/{quoteId}/status/{status}", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    /// <summary>Real send: renders a PDF and emails it to the customer on file, Draft-only.</summary>
    public async Task<Quote?> SendQuoteAsync(int quoteId)
    {
        var response = await _http.PostAsync($"api/quotes/{quoteId}/send", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Quote>(JsonOptions);
    }

    /// <summary>Reverts a Sent quote back to Draft so its lines become editable again.</summary>
    public async Task<Quote?> ReviseQuoteAsync(int quoteId)
    {
        var response = await _http.PostAsync($"api/quotes/{quoteId}/revise", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Quote>(JsonOptions);
    }

    public async Task<Invoice?> ConvertQuoteToInvoiceAsync(int quoteId)
    {
        var response = await _http.PostAsync($"api/quotes/{quoteId}/convert-to-invoice", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Invoice>(JsonOptions);
    }

    public async Task DeleteQuoteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/quotes/{id}");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    // Payments
    public Task<List<Payment>?> GetPaymentsAsync()
        => _http.GetFromJsonAsync<List<Payment>>("api/payments", JsonOptions);

    public Task<Payment?> GetPaymentAsync(int id)
        => _http.GetFromJsonAsync<Payment>($"api/payments/{id}", JsonOptions);

    public async Task<Payment?> CreatePaymentAsync(Payment payment)
    {
        var response = await _http.PostAsJsonAsync("api/payments", payment);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Payment>(JsonOptions);
    }

    public async Task AllocatePaymentAsync(int paymentId, int invoiceId, decimal amount)
    {
        var response = await _http.PostAsJsonAsync($"api/payments/{paymentId}/allocations",
            new CreateAllocationRequest { InvoiceId = invoiceId, AllocatedAmount = amount });
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }

    public async Task ReverseAllocationAsync(int allocationId)
    {
        var response = await _http.PostAsync($"api/payments/allocations/{allocationId}/reverse", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
    }
}
