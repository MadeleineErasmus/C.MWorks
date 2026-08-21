using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobCardApp.Mobile.Services;
using JobCardApp.Shared.Models;

namespace JobCardApp.Mobile.ViewModels;

[QueryProperty(nameof(CompanyId), "id")]
public partial class CompanyEditViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private int companyId;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? phone;
    [ObservableProperty] private string? email;

    [ObservableProperty] private bool isVatRegistered = true;
    [ObservableProperty] private string? vatNumber;
    [ObservableProperty] private string taxRatePercent = "15";

    // Per-company job pricing defaults — prefilled onto a job card's call-out
    // and labour lines. Kept as strings for the same reason as TaxRatePercent:
    // an Entry binds to text, and a half-typed value must not throw.
    [ObservableProperty] private string defaultCallOutFeeText = "0";
    [ObservableProperty] private string defaultLabourRateText = "0";

    [ObservableProperty] private string? bankName;
    [ObservableProperty] private string? accountHolder;
    [ObservableProperty] private string? accountNumber;
    [ObservableProperty] private string? branchCode;
    [ObservableProperty] private string? accountType;

    [ObservableProperty] private bool isBusy;

    public bool CanDelete => CompanyId > 0;

    public CompanyEditViewModel(ApiClient api) => _api = api;

    partial void OnCompanyIdChanged(int value) => _ = LoadAsync();

    // A non-VAT company is always 0% — matches what the server enforces.
    partial void OnIsVatRegisteredChanged(bool value)
    {
        if (!value) TaxRatePercent = "0";
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (CompanyId <= 0) return;

        IsBusy = true;
        try
        {
            var company = await _api.GetCompanyAsync(CompanyId);
            if (company is not null)
            {
                Name = company.Name;
                Address = company.Address;
                Phone = company.Phone;
                Email = company.Email;
                IsVatRegistered = company.IsVatRegistered;
                VatNumber = company.VatNumber;
                TaxRatePercent = (company.TaxRate * 100).ToString("0.##");
                DefaultCallOutFeeText = company.DefaultCallOutFee.ToString("0.##");
                DefaultLabourRateText = company.DefaultLabourRate.ToString("0.##");
                BankName = company.BankName;
                AccountHolder = company.AccountHolder;
                AccountNumber = company.AccountNumber;
                BranchCode = company.BranchCode;
                AccountType = company.AccountType;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Load failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Missing details", "Enter a company name.", "OK");
            return;
        }

        decimal.TryParse(TaxRatePercent, out var pct);
        // A blank or unparseable rate means "not configured" — 0, which the
        // job card screen reads as "fall back to its own default".
        decimal.TryParse(DefaultCallOutFeeText, out var callOutFee);
        decimal.TryParse(DefaultLabourRateText, out var labourRate);

        var company = new Company
        {
            Id = CompanyId,
            Name = Name.Trim(),
            Address = Address,
            Phone = Phone,
            Email = Email,
            IsVatRegistered = IsVatRegistered,
            VatNumber = VatNumber,
            TaxRate = IsVatRegistered ? pct / 100m : 0m,
            DefaultCallOutFee = callOutFee < 0 ? 0m : callOutFee,
            DefaultLabourRate = labourRate < 0 ? 0m : labourRate,
            BankName = BankName,
            AccountHolder = AccountHolder,
            AccountNumber = AccountNumber,
            BranchCode = BranchCode,
            AccountType = AccountType,
            IsActive = true
        };

        IsBusy = true;
        try
        {
            if (CompanyId > 0)
                await _api.UpdateCompanyAsync(company);
            else
                await _api.CreateCompanyAsync(company);

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Save failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Delete company", $"Delete {Name}? This cannot be undone.", "Delete", "Cancel");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _api.DeleteCompanyAsync(CompanyId);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Delete failed", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
