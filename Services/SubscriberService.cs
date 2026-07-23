using IBS.Data;
using IBSMobile.Contracts;
using IBSMobile.Data;
using IBSMobile.DTOs;
using IBSMobile.Functions;
using IBSMobile.Models;
using IBSMobile.Statics;
using IBSMobile.Tenancy;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace IBSMobile.Services;

public class SubscriberService : ISubscriberService
{
    private const string AffiliateEarthlink = "earthlink";
    private const string AffiliateSas = "sas";
    private const string AffiliateFtth = "ftth";

    private readonly ApplicationDbContext _context;
    private readonly IBSFunctions _function;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantConnectionAccessor _tenant;
    private readonly string _qiBaseUri;
    private readonly string _qiSucceedRedirect;
    private readonly string _qiCreatePath;
    private readonly string _qiStatusPath;
    private readonly string _qiToken;

    private int? _cachedUserAppUserId;
    private int? _cachedCashAccountId;

    public SubscriberService(
        IConfiguration configuration,
        ApplicationDbContext db,
        IBSFunctions function,
        IHttpClientFactory httpClientFactory,
        ITenantConnectionAccessor tenant)
    {
        _context = db;
        _function = function;
        _httpClientFactory = httpClientFactory;
        _tenant = tenant;
        _qiBaseUri = configuration["QiCard:BaseUri"] ?? "";
        _qiSucceedRedirect = ResolveQiReturnUrl(
            configuration["QiCard:returnUrl"],
            configuration["QiCard:FinishPaymentUrl"]);
        _qiCreatePath = configuration["QiCard:CreatePath"] ?? "/api/Payment/CreateTest";
        _qiStatusPath = configuration["QiCard:StatusPath"] ?? "/api/Payment/StatusTest";
        _qiToken = configuration["QiCard:Token"] ?? "ccd589cc-fd7c-4597-86d4-8a8b62b0573b";
    }

    /// <summary>
    /// Prefer QiCard:returnUrl from appsettings. Accepts host (localhost:5173) or full URL.
    /// </summary>
    private static string ResolveQiReturnUrl(string? returnUrl, string? finishPaymentUrl)
    {
        var raw = !string.IsNullOrWhiteSpace(returnUrl)
            ? returnUrl.Trim()
            : (finishPaymentUrl ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(raw))
            return "http://localhost:5173/payment/notification";

        if (!raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            raw = "http://" + raw.TrimStart('/');
        }

        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/")
                return $"{uri.Scheme}://{uri.Authority}/payment/notification";

            return raw.TrimEnd('/');
        }

        return raw.TrimEnd('/');
    }

    private string BuildQiCallbackUrl()
    {
        var baseUrl = _qiSucceedRedirect.TrimEnd('/');
        var company = _tenant.CompanyKey;
        if (string.IsNullOrWhiteSpace(company))
            return baseUrl;

        // Put company in the PATH (not query). Qi appends its own params with "?"
        // which corrupts values like company=KGD?requestId=... when we use query string.
        if (baseUrl.Contains("/payment/notification", StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = baseUrl.Split('?', 2)[0].TrimEnd('/');
            return $"{trimmed}/{Uri.EscapeDataString(company)}";
        }

        return $"{baseUrl.Split('?', 2)[0].TrimEnd('/')}/{Uri.EscapeDataString(company)}";
    }

    private string BuildPaymentRequestId()
    {
        var guid = Guid.NewGuid().ToString("N");
        var company = _tenant.CompanyKey;
        // Backup channel: company survives even if return URL parsing fails.
        return string.IsNullOrWhiteSpace(company)
            ? guid
            : $"{company.Trim().ToUpperInvariant()}.{guid}";
    }

    private static string? ExtractCompanyFromRequestId(string? requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return null;

        var dot = requestId.IndexOf('.');

        if (dot <= 0 || dot >= requestId.Length - 1)
            return null;

        var prefix = requestId[..dot].Trim();

        return prefix.Length is >= 2 and <= 32 ? prefix : null;
    }

    private static string NormalizeAffiliateType(string? affiliateType) =>
        affiliateType?.Trim().ToLowerInvariant() ?? string.Empty;

    private async Task<int> GetUserAppUserIdAsync()
    {
        if (_cachedUserAppUserId.HasValue)
            return _cachedUserAppUserId.Value;

        var userApp = await _context.UserApp.AsNoTracking().FirstOrDefaultAsync();
        if (userApp == null)
            throw new InvalidOperationException("اعدادت التطبيق غير صحيحة.");

        _cachedUserAppUserId = userApp.UserId;
        _cachedCashAccountId = userApp.CashAccountId;
        return userApp.UserId;
    }

    private async Task<int> GetUserAppCashAccountIdAsync()
    {
        if (_cachedCashAccountId.HasValue)
            return _cachedCashAccountId.Value;

        await GetUserAppUserIdAsync();
        return _cachedCashAccountId!.Value;
    }

    public async Task<DtoFinancialInfo> FinancialInfoAsync(int userId)
    {
        var credit = await _context.SubscribersCredits
            .AsNoTracking()
            .Where(m => m.SubscId == userId)
            .Select(m => m.current_credit)
            .FirstOrDefaultAsync();

        var mobileApp = await _context.SubscriberApp
            .AsNoTracking()
            .Where(m => m.SubscId == userId)
            .Select(m => new { m.CanActiveNoCash, m.AmountDue })
            .FirstOrDefaultAsync();

        var lastActivation = await _context.Activation_Users
            .AsNoTracking()
            .Where(m => m.FK_Activation_Activation_SubscId.Id == userId)
            .OrderByDescending(m => m.activation_date)
            .Select(m => (DateTime?)m.activation_date)
            .FirstOrDefaultAsync();

        return new DtoFinancialInfo
        {
            userId = userId,
            AmountDue = credit,
            DebtLimit = mobileApp?.AmountDue,
            CanActiveNoCash = mobileApp?.CanActiveNoCash ?? false,
            LastActivation = lastActivation
        };
    }

    public async Task<Response> SubscriptionInfoAsync(int userId)
    {
        var subscriber = await _context.Subscribers
            .Include(x => x.FK_Subscribers_MainAffiliate)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (subscriber?.FK_Subscribers_MainAffiliate == null)
            return _function.ErrorResponse("اسم المستخدم غير صحيح");

        var userData = await FetchAffiliateUserDataAsync(subscriber);
        if (userData == null || userData.error)
            return _function.ErrorResponse(userData?.message ?? "حدث خطأ ما");

        SASAppUserResponse userInfo;
        try
        {
            userInfo = JsonConvert.DeserializeObject<SASAppUserResponse>(userData.data?.ToString() ?? "")
                ?? new SASAppUserResponse();
        }
        catch
        {
            userInfo = new SASAppUserResponse();
        }

        return _function.SuccessResponse(userInfo);
    }

    public async Task<List<DtoProfilePackage>> GetPackagesAsync(int userId)
    {
        var subscriber = await _context.Subscribers
            .AsNoTracking()
            .Where(s => s.Id == userId)
            .Select(s => new
            {
                MainAffiliateId = EF.Property<int>(s, "MainAffiliate"),
                SubAffiliateId = EF.Property<int>(s, "SubAffiliate"),
            })
            .FirstOrDefaultAsync();

        if (subscriber == null)
            return [];

        var profiles = await _context.Profiles
            .AsNoTracking()
            .Where(p => p.Account_Active && p.Account_MainAffiliate == subscriber.MainAffiliateId)
            .OrderBy(p => p.Account_Price)
            .ThenBy(p => p.Account_Name)
            .ToListAsync();

        if (profiles.Count == 0)
            return [];

        var profileIds = profiles.Select(p => p.Id).ToList();

        var appCosts = await _context.ProfileAppCosts
            .AsNoTracking()
            .Where(c => profileIds.Contains(c.ProfileId))
            .ToDictionaryAsync(c => c.ProfileId, c => c.Price);

        var subCosts = await _context.ProfileCosts
            .AsNoTracking()
            .Where(c => c.SubAffiliateId == subscriber.SubAffiliateId && profileIds.Contains(c.ProfileId))
            .ToDictionaryAsync(c => c.ProfileId, c => c.SaleCost);

        return profiles.Select(p =>
        {
            decimal price = p.Account_Price;
            if (appCosts.TryGetValue(p.Id, out var appPrice))
                price = appPrice;
            else if (subCosts.TryGetValue(p.Id, out var saleCost))
                price = saleCost;

            return new DtoProfilePackage
            {
                Id = p.Id,
                AccIndex = p.AccIndex,
                Name = p.Account_Name,
                Description = p.Account_Description,
                BuyCost = p.Account_BuyCost,
                Price = price,
            };
        }).ToList();
    }

    public async Task<Response> RefillSubscriptionAsync(int userId, bool saleType, int profileId)
    {
        var userAppUserId = await GetUserAppUserIdAsync();

        var credit = await _context.SubscribersCredits
            .Where(m => m.SubscId == userId)
            .Select(u => u.current_credit)
            .FirstOrDefaultAsync();

        var userDiscount = await _context.SubscriberDiscounts
            .AsNoTracking()
            .Where(m => m.SubscId == userId)
            .Select(i => i.Amount)
            .FirstOrDefaultAsync();

        var profile = await _context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == profileId);

        if (profile == null)
            return _function.ErrorResponse("نوع الاشتراك غير صحيح");

        SubscriberApp? subscApp = null;
        if (!saleType)
        {
            subscApp = await _context.SubscriberApp
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.SubscId == userId);
        }

        decimal debtAmount = 0;
        if (!saleType)
        {
            if (subscApp != null && !subscApp.CanActiveNoCash)
                return _function.ErrorResponse("لا يمكنك تفعيل الاشتراك آجل");

            debtAmount = subscApp?.AmountDue ?? 0;
            if (credit + (profile.Account_BuyCost - userDiscount) > debtAmount)
                return _function.ErrorResponse("لا يمكنك تفعيل الاشتراك آجل لتجازوك الحد المسموح بالدين");
        }

        var cashId = 0;
        if (saleType)
        {
            cashId = await GetUserAppCashAccountIdAsync();
            if (cashId == 0)
                return _function.ErrorResponse("الحساب النقدي غير محدد. يجب الاتصال بالوكيل لغرض تحديد الحساب النقدي");
        }

        var affiliateType = await (
            from s in _context.Subscribers.AsNoTracking()
            join ma in _context.MainAffiliates.AsNoTracking()
                on EF.Property<int>(s, "MainAffiliate") equals ma.Id
            where s.Id == userId
            select ma.AffiliateType
        ).FirstOrDefaultAsync();

        if (affiliateType is null)
            return _function.ErrorResponse("اسم المستخدم غير صحيح");

        if (NormalizeAffiliateType(affiliateType) == AffiliateFtth)
            return _function.ErrorResponse("لا يمكنك تفعيل اشتراكك الآن. يرجى المحاولة لاحقاً");

        var subscriber = await LoadSubscriberWithAffiliatesAsync(userId);
        if (subscriber == null)
            return _function.ErrorResponse("اسم المستخدم غير صحيح");

        var conStr = await GetEncryptedConnectionStringAsync();
        return await ExecuteRefillAsync(
            subscriber,
            profile,
            userDiscount,
            saleType,
            cashId,
            userAppUserId,
            conStr);
    }

    public async Task<decimal> GetAmountDue(int userId) =>
        await _context.SubscribersCredits
            .AsNoTracking()
            .Where(m => m.SubscId == userId)
            .Select(u => u.current_credit)
            .FirstOrDefaultAsync();

    public async Task<List<DtoActivation>> GetInvoicesAsync(int userId) =>
        await _context.Activation_Users
            .Include(u => u.FK_Activation_Profile)
            .AsNoTracking()
            .Where(m => m.FK_Activation_Activation_SubscId.Id == userId)
            .Select(x => new DtoActivation
            {
                id = x.Id,
                creationDate = x.activation_date,
                saleType = x.Activation_SaleType,
                cost = x.Activation_Cost,
                profile = x.FK_Activation_Profile.Account_Description
            })
            .ToListAsync();

    public async Task<List<DtoReceivable>> GetReceivableAsync(int userId) =>
        await _context.Receivables
            .AsNoTracking()
            .Where(m => m.FK_Receivables_Rec_SubscId.Id == userId)
            .Select(x => new DtoReceivable
            {
                id = x.RecId,
                creationDate = x.Rec_Date,
                amount = x.Rec_Amount
            })
            .ToListAsync();

    public async Task<Response> PayBackAmountAsync(int userId)
    {
        var amountDue = await GetAmountDue(userId);
        if (amountDue <= 0)
            return _function.SuccessResponse("لا يوجد مبلغ مستحق");

        var cashId = await GetUserAppCashAccountIdAsync();
        if (cashId == 0)
            return _function.ErrorResponse("الحساب النقدي غير محدد. يجب الاتصال بالوكيل");

        var userAppUserId = await GetUserAppUserIdAsync();

        var subscExists = await _context.Subscribers
            .AsNoTracking()
            .AnyAsync(m => m.IsValid && m.Id == userId);

        if (!subscExists)
            return _function.ErrorResponse("حساب المشترك غير موجود أو غير فعال");

        var cashExists = await _context.Chart_Accounts
            .AsNoTracking()
            .AnyAsync(m => m.Ch_Id == cashId);

        if (!cashExists)
            return _function.ErrorResponse("الحساب النقدي غير محدد. يجب الاتصال بالوكيل");

        var empExists = await _context.User
            .AsNoTracking()
            .AnyAsync(i => i.Id == userAppUserId);

        if (!empExists)
            return _function.ErrorResponse("حساب الموضف غير محدد. يجب الاتصال بالوكيل");

        var receivable = new Receivable
        {
            Rec_Date = DateTime.Now,
            Rec_Amount = amountDue,
            Rec_SubscId = userId,
            Rec_Cash_Account = cashId,
            Rec_empid = userAppUserId,
            Rec_Note = "تم الاستلام من قبل التطبيق"
        };

        await _context.Receivables.AddAsync(receivable);
        await _context.SaveChangesAsync();

        return _function.SuccessResponse("تم تسديد المبلغ. المبلغ المطلوب صفر");
    }

    public async Task<Response> CreatePaymentAsync(
        int subscriberId,
        decimal amount,
        int? profileId = null,
        bool saleType = true,
        string? purpose = null,
        string? returnUrl = null)
    {
        if (amount <= 0)
            return _function.ErrorResponse("المبلغ غير صالح");

        // Prefer explicit purpose; otherwise infer Debt when no package is selected.
        var normalizedPurpose =
            string.Equals(purpose, "Debt", StringComparison.OrdinalIgnoreCase) ||
            (string.IsNullOrWhiteSpace(purpose) && (profileId is null or <= 0))
                ? "Debt"
                : "Refill";

        var subscriber = await _context.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IsValid && m.Id == subscriberId);

        if (subscriber == null)
            return _function.ErrorResponse("حساب المشترك غير موجود أو غير فعال");

        if (normalizedPurpose == "Debt")
        {
            var amountDue = await GetAmountDue(subscriberId);
            if (amountDue <= 0)
                return _function.ErrorResponse("لا يوجد مبلغ دين مستحق للدفع");
        }

        if (normalizedPurpose == "Refill")
        {
            if (profileId is null or <= 0)
                return _function.ErrorResponse("يجب اختيار الباقة");

            var profile = await _context.Profiles
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == profileId && m.Account_Active);

            if (profile == null)
                return _function.ErrorResponse("نوع الاشتراك غير صحيح");
        }

        var requestId = BuildPaymentRequestId();
        var payment = new Payment
        {
            SubscriberId = subscriberId,
            Amount = amount,
            Currency = "IQD",
            Status = PaymentStatus.Pending,
            Type = PaymentType.OneTime,
            RequestId = requestId,
            Purpose = normalizedPurpose,
            ProfileId = normalizedPurpose == "Refill" ? profileId : null,
            SaleType = saleType,
            RefillExecuted = false,
            CreatedAt = DateTime.Now
        };

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var callbackUrl = BuildQiCallbackUrl();
        var body = new DtoCreatePaymentObject
        {
            token = _qiToken,
            amount = amount,
            AppId = "IBS-MOBILE",
            currency = "IQD",
            customerInfo = BuildPaymentCustomerInfo(subscriber),
            requestId = requestId,
            returnUrl = callbackUrl
        };

        var (qiPayment, qiError) = await CallQiPaymentApiAsync(body);
        if (qiError != null)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return _function.ErrorResponse(qiError);
        }

        payment.PaymentId = qiPayment!.qiPaymentId;
        if (!string.IsNullOrWhiteSpace(qiPayment.requestId))
            payment.RequestId = qiPayment.requestId;
        payment.ModifiedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return _function.SuccessResponse(new
        {
            PaymentId = payment.Id,
            FormUrl = qiPayment.formUrl,
            QiPaymentId = qiPayment.qiPaymentId,
            RequestId = payment.RequestId,
            Amount = amount,
            Currency = "IQD",
            ProfileId = payment.ProfileId,
            Purpose = payment.Purpose
        });
    }

    public async Task<Response> ConfirmPaymentAsync(
        int? subscriberId,
        string qiPaymentId,
        string? requestId = null,
        string? statusHint = null)
    {
        if (string.IsNullOrWhiteSpace(qiPaymentId))
            return _function.ErrorResponse("معرف الدفع غير صالح");

        var paymentQuery = _context.Payments.AsQueryable();

        if (subscriberId is > 0)
        {
            paymentQuery = paymentQuery.Where(p => p.SubscriberId == subscriberId.Value);
        }

        var payment = await paymentQuery.FirstOrDefaultAsync(p =>
            p.PaymentId == qiPaymentId ||
            (!string.IsNullOrWhiteSpace(requestId) && p.RequestId == requestId));

        if (payment == null)
            return _function.ErrorResponse("عملية الدفع غير موجودة");

        // Prefer the subscriber tied to the payment row (works for anonymous return-url confirm).
        subscriberId = payment.SubscriberId;

        var isDebt = string.Equals(payment.Purpose, "Debt", StringComparison.OrdinalIgnoreCase);

        if (payment.Status == PaymentStatus.Succeeded && payment.RefillExecuted)
        {
            return _function.SuccessResponse(new
            {
                Message = isDebt
                    ? "تم تأكيد دفع الدين مسبقاً"
                    : "تم تأكيد الدفع وتجديد الاشتراك مسبقاً",
                Purpose = payment.Purpose ?? "Refill"
            });
        }

        var (statusData, statusError) = await CallQiPaymentStatusAsync(qiPaymentId);
        if (statusError != null)
            return _function.ErrorResponse(statusError);

        var qiStatus = statusData!.status?.Trim().ToUpperInvariant() ?? "";
        var canceled = statusData.canceled;

        if (canceled || qiStatus is "FAILED" or "CANCELED" or "CANCELLED")
        {
            payment.Status = canceled || qiStatus.Contains("CANCEL") ? PaymentStatus.Canceled : PaymentStatus.Failed;
            payment.FailureReason = statusData.status ?? statusHint ?? "فشل الدفع";
            payment.PaymentMethod = statusData.paymentType;
            payment.ModifiedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return _function.ErrorResponse(payment.FailureReason);
        }

        if (qiStatus != "SUCCESS")
        {
            payment.Status = PaymentStatus.Processing;
            payment.ModifiedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return _function.ErrorResponse($"الدفع لم يكتمل بعد. الحالة: {statusData.status}");
        }

        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = DateTime.Now;
        payment.PaymentMethod = statusData.paymentType;
        payment.IsReceivedFromUfeq = true;
        payment.ReceivingDate = DateTime.Now;
        payment.ModifiedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(statusData.requestId))
            payment.RequestId = statusData.requestId;
        await _context.SaveChangesAsync();

        if (payment.RefillExecuted)
        {
            return _function.SuccessResponse(new
            {
                Message = isDebt
                    ? "تم دفع الدين بنجاح مسبقاً"
                    : "تم الدفع بنجاح وتم تجديد الاشتراك مسبقاً",
                Purpose = payment.Purpose ?? "Refill"
            });
        }

        if (isDebt)
        {
            var debtResult = await ExecutePaidDebtAsync(subscriberId.Value, payment.Amount);
            if (debtResult.error)
            {
                payment.FailureReason = debtResult.message ?? "فشل تسجيل تسديد الدين بعد الدفع";
                payment.ModifiedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return debtResult;
            }

            payment.RefillExecuted = true;
            payment.FailureReason = null;
            payment.ModifiedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return _function.SuccessResponse(new
            {
                Message = debtResult.message
                    ?? ExtractResponseText(debtResult)
                    ?? "تم دفع الدين بنجاح",
                Purpose = "Debt"
            });
        }

        if (payment.ProfileId is null or <= 0)
            return _function.ErrorResponse("تم الدفع لكن الباقة غير مرتبطة بعملية الدفع");

        var refillResult = await ExecutePaidRefillAsync(subscriberId.Value, payment.ProfileId.Value, payment.SaleType);
        if (refillResult.error)
        {
            payment.FailureReason = refillResult.message ?? "فشل التجديد بعد الدفع";
            payment.ModifiedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return refillResult;
        }

        payment.RefillExecuted = true;
        payment.FailureReason = null;
        payment.ModifiedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return _function.SuccessResponse(new
        {
            Message = LocalizeAffiliateMessage(
                ExtractResponseText(refillResult) ?? "تم الدفع وتجديد الاشتراك بنجاح"),
            Purpose = "Refill"
        });
    }

    private async Task<Response> ExecutePaidDebtAsync(int userId, decimal paidAmount)
    {
        if (paidAmount <= 0)
            return _function.ErrorResponse("مبلغ الدين المدفوع غير صالح");

        var cashId = await GetUserAppCashAccountIdAsync();
        if (cashId == 0)
            return _function.ErrorResponse("الحساب النقدي غير محدد. يجب الاتصال بالوكيل");

        var userAppUserId = await GetUserAppUserIdAsync();

        var subscExists = await _context.Subscribers
            .AsNoTracking()
            .AnyAsync(m => m.IsValid && m.Id == userId);

        if (!subscExists)
            return _function.ErrorResponse("حساب المشترك غير موجود أو غير فعال");

        var cashExists = await _context.Chart_Accounts
            .AsNoTracking()
            .AnyAsync(m => m.Ch_Id == cashId);

        if (!cashExists)
            return _function.ErrorResponse("الحساب النقدي غير محدد. يجب الاتصال بالوكيل");

        var empExists = await _context.User
            .AsNoTracking()
            .AnyAsync(i => i.Id == userAppUserId);

        if (!empExists)
            return _function.ErrorResponse("حساب الموظف غير محدد. يجب الاتصال بالوكيل");

        // Set FK ids only — do not attach AsNoTracking navigations (causes IDENTITY inserts).
        var receivable = new Receivable
        {
            Rec_Date = DateTime.Now,
            Rec_Amount = paidAmount,
            Rec_SubscId = userId,
            Rec_Cash_Account = cashId,
            Rec_empid = userAppUserId,
            Rec_Note = "تم الاستلام إلكترونياً من التطبيق"
        };

        await _context.Receivables.AddAsync(receivable);
        await _context.SaveChangesAsync();

        return _function.SuccessResponse($"تم تسديد مبلغ {paidAmount:N0} دينار بنجاح");
    }

    private static string? ExtractResponseText(Response result)
    {
        if (!string.IsNullOrWhiteSpace(result.message))
            return result.message;

        return result.data switch
        {
            string s when !string.IsNullOrWhiteSpace(s) => s,
            _ => null
        };
    }

    private static string LocalizeAffiliateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "تم الدفع وتجديد الاشتراك بنجاح";

        // Earthlink/SAS English success: "... next user expiration date is 16/08/2026 01:13 AM"
        var expirationMatch = System.Text.RegularExpressions.Regex.Match(
            message,
            @"expiration date is\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (message.Contains("payment was accepted", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("account has been updated", StringComparison.OrdinalIgnoreCase))
        {
            var datePart = expirationMatch.Success
                ? expirationMatch.Groups[1].Value.Trim().Replace(" AM", " ص").Replace(" PM", " م")
                : null;

            return string.IsNullOrWhiteSpace(datePart)
                ? "تم قبول الدفع وتحديث حسابك بنجاح"
                : $"تم قبول الدفع وتحديث حسابك بنجاح. تاريخ انتهاء الاشتراك القادم: {datePart}";
        }

        return message;
    }

    private async Task<Response> ExecutePaidRefillAsync(int userId, int profileId, bool saleType)
    {
        var userAppUserId = await GetUserAppUserIdAsync();
        var cashId = await GetUserAppCashAccountIdAsync();
        if (cashId == 0)
            return _function.ErrorResponse("الحساب النقدي غير محدد. يجب الاتصال بالوكيل لغرض تحديد الحساب النقدي");

        var profile = await _context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == profileId);

        if (profile == null)
            return _function.ErrorResponse("نوع الاشتراك غير صحيح");

        var userDiscount = await _context.SubscriberDiscounts
            .AsNoTracking()
            .Where(m => m.SubscId == userId)
            .Select(i => i.Amount)
            .FirstOrDefaultAsync();

        var subscriber = await LoadSubscriberWithAffiliatesAsync(userId);
        if (subscriber == null)
            return _function.ErrorResponse("اسم المستخدم غير صحيح");

        if (NormalizeAffiliateType(subscriber.FK_Subscribers_MainAffiliate.AffiliateType) == AffiliateFtth)
            return _function.ErrorResponse("لا يمكنك تفعيل اشتراكك الآن. يرجى المحاولة لاحقاً");

        var conStr = await GetEncryptedConnectionStringAsync();
        return await ExecuteRefillAsync(
            subscriber,
            profile,
            userDiscount,
            saleType,
            cashId,
            userAppUserId,
            conStr);
    }

    private async Task<Subscriber?> LoadSubscriberWithAffiliatesAsync(int userId)
    {
        var keys = await _context.Subscribers
            .AsNoTracking()
            .Where(s => s.Id == userId)
            .Select(s => new
            {
                MainId = EF.Property<int>(s, "MainAffiliate"),
                SubId = EF.Property<int>(s, "SubAffiliate"),
            })
            .FirstOrDefaultAsync();

        if (keys == null)
            return null;

        var subscriber = await _context.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == userId);

        if (subscriber == null)
            return null;

        var main = await _context.MainAffiliates
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == keys.MainId);

        var sub = await _context.SubAffiliates
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == keys.SubId);

        if (main == null || sub == null)
            return null;

        subscriber.FK_Subscribers_MainAffiliate = main;
        subscriber.FK_Subscribers_SubAffiliate = sub;
        return subscriber;
    }

    private async Task<Response?> FetchAffiliateUserDataAsync(Subscriber subscriber)
    {
        var affiliate = subscriber.FK_Subscribers_MainAffiliate;
        var affiliateId = affiliate.Id;
        var affiliateType = NormalizeAffiliateType(affiliate.AffiliateType);

        return affiliateType switch
        {
            AffiliateEarthlink when subscriber.UserIndex == 0 =>
                await _function.EarthlinkGetUserDataByUsernameWithPassword(affiliateId, subscriber.Username),
            AffiliateEarthlink =>
                await _function.EarthlinkGetUserDataByIndexWithPassword(affiliateId, subscriber.UserIndex),
            AffiliateSas when subscriber.UserIndex == 0 =>
                await _function.SASGetUserDataByUsername(affiliateId, affiliate.Uri, subscriber.Username),
            AffiliateSas =>
                await _function.SASGetUserDataByIndex(affiliateId, affiliate.Uri, subscriber.UserIndex),
            AffiliateFtth =>
                await _function.FTTHGetUserDataByIndex(affiliateId, subscriber.UserIndex, 1),
            _ => null
        };
    }

    private async Task<string> GetEncryptedConnectionStringAsync()
    {
        var dbConnect = _context.Database.GetConnectionString() ?? string.Empty;
        return await Encryptions.EncryptDatabase(dbConnect);
    }

    private async Task<Response> ExecuteRefillAsync(
        Subscriber subscriber,
        Profile profile,
        decimal userDiscount,
        bool saleType,
        int cashId,
        int userAppUserId,
        string conStr)
    {
        var affiliateType = NormalizeAffiliateType(subscriber.FK_Subscribers_MainAffiliate.AffiliateType);
        var cost = profile.Account_Price - userDiscount;

        return affiliateType switch
        {
            AffiliateEarthlink => await RefillEarthlinkAsync(subscriber, profileId: profile.Id, cost, saleType, cashId, userAppUserId, conStr),
            AffiliateSas => await RefillSasAsync(subscriber, profile, cost, saleType, cashId, userAppUserId, conStr),
            _ => _function.ErrorResponse("الصفحة الرئيسية غير معرفة")
        };
    }

    private async Task<Response> RefillEarthlinkAsync(
        Subscriber subscriber,
        int profileId,
        decimal cost,
        bool saleType,
        int cashId,
        int userAppUserId,
        string conStr)
    {
        var affiliate = subscriber.FK_Subscribers_MainAffiliate;
        var earthForm = new DtoRefillWithDeposit
        {
            DepositPassword = affiliate.DepositPassword,
            connectionString = conStr,
            username = subscriber.Username,
            agent = 0,
            userId = subscriber.Id,
            emp = userAppUserId,
            details = "تم التفعيل من قبل المشترك",
            profileId = profileId,
            subAffiliate = subscriber.FK_Subscribers_SubAffiliate.Id,
            saleType = saleType,
            cost = cost,
            received = 0,
            cashAccount = cashId,
            saveInvoice = true
        };

        var result = await _function.EarthRefillUser(earthForm, affiliate.Id);
        return result ?? _function.ErrorResponse("حدث خطأ أثناء تفعيل المشترك");
    }

    private async Task<Response> RefillSasAsync(
        Subscriber subscriber,
        Profile profile,
        decimal cost,
        bool saleType,
        int cashId,
        int userAppUserId,
        string conStr)
    {
        var affiliate = subscriber.FK_Subscribers_MainAffiliate;
        var sasForm = new DtoSASActiveViaDeposit
        {
            uri = affiliate.Uri,
            connectionString = conStr,
            userIndex = subscriber.UserIndex,
            agent = 0,
            userId = subscriber.Id,
            username = subscriber.Username,
            emp = userAppUserId,
            details = "تم التفعيل من قبل المشترك",
            profileId = profile.AccIndex,
            subAffiliate = subscriber.FK_Subscribers_SubAffiliate.Id,
            saleType = saleType,
            cost = cost,
            received = 0,
            cashAccount = cashId,
            saveInvoice = true
        };

        var result = await _function.SASRefillUser(sasForm, affiliate.Id);
        return result ?? _function.ErrorResponse("حدث خطأ أثناء تفعيل المشترك");
    }

    private static DtoPaymentCustomerObj BuildPaymentCustomerInfo(Subscriber subscriber) =>
        new()
        {
            firstName = subscriber.NameStr ?? subscriber.Username,
            middleName = "",
            lastName = "",
            phone = subscriber.Mobile.ToString(),
            email = subscriber.Email ?? "",
            address = subscriber.Address ?? ""
        };

    private async Task<(DtoPaymentReturn? qiPayment, string? errorMessage)> CallQiPaymentApiAsync(DtoCreatePaymentObject body)
    {
        if (string.IsNullOrWhiteSpace(_qiBaseUri))
            return (null, "إعدادات الدفع الإلكتروني غير صحيحة");

        var path = _qiCreatePath.StartsWith('/') ? _qiCreatePath : "/" + _qiCreatePath;
        using var httpClient = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_qiBaseUri.TrimEnd('/')}{path}");
        request.Content = new StringContent(
            JsonConvert.SerializeObject(body),
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (null, $"خطأ في QiCard: {responseContent}");

        var qiPayment = JsonConvert.DeserializeObject<DtoDtoPaymentReturnResponse>(responseContent);
        if (qiPayment?.error == true)
            return (null, qiPayment.message ?? $"خطأ في QiCard: {responseContent}");

        if (string.IsNullOrWhiteSpace(qiPayment?.data?.formUrl))
            return (null, "فشل إنشاء نموذج الدفع من QiCard");

        return (qiPayment!.data, null);
    }

    private async Task<(DtoPaymentResponseStatus? status, string? errorMessage)> CallQiPaymentStatusAsync(string qiPaymentId)
    {
        if (string.IsNullOrWhiteSpace(_qiBaseUri))
            return (null, "إعدادات الدفع الإلكتروني غير صحيحة");

        var path = _qiStatusPath.StartsWith('/') ? _qiStatusPath : "/" + _qiStatusPath;
        using var httpClient = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_qiBaseUri.TrimEnd('/')}{path.TrimEnd('/')}/{Uri.EscapeDataString(qiPaymentId)}");

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (null, $"خطأ في التحقق من حالة الدفع: {responseContent}");

        var parsed = JsonConvert.DeserializeObject<DtoStatusResponse>(responseContent);
        if (parsed?.error == true)
            return (null, parsed.message ?? "فشل التحقق من حالة الدفع");

        if (parsed?.data == null)
            return (null, "لم يتم العثور على بيانات حالة الدفع");

        return (parsed.data, null);
    }
}
