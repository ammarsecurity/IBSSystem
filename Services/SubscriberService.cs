using IBS.Data;
using IBSMobile.Contracts;
using IBSMobile.Data;
using IBSMobile.DTOs;
using IBSMobile.Functions;
using IBSMobile.Models;
using IBSMobile.Statics;
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
    private readonly string _qiBaseUri;
    private readonly string _qiSucceedRedirect;

    private int? _cachedUserAppUserId;
    private int? _cachedCashAccountId;

    public SubscriberService(
        IConfiguration configuration,
        ApplicationDbContext db,
        IBSFunctions function,
        IHttpClientFactory httpClientFactory)
    {
        _context = db;
        _function = function;
        _httpClientFactory = httpClientFactory;
        _qiBaseUri = configuration["QiCard:BaseUri"] ?? "";
        _qiSucceedRedirect = configuration["QiCard:FinishPaymentUrl"] ?? "";
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

        return _function.SuccessResponse("تم التفعيل بنجاح");

        // لا تغير شي بالجوه عوفه هيج
        //var conStr = await GetEncryptedConnectionStringAsync();
        //var userDiscountAmount = await discountTask;

        //return await ExecuteRefillAsync(
        //    subscriber,
        //    profile,
        //    userDiscountAmount,
        //    saleType,
        //    cashId,
        //    userAppUserId,
        //    conStr);
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

        var userAppUserId = await GetUserAppUserIdAsync();

        var subsc = await _context.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IsValid && m.Id == userId);

        if (subsc == null)
            return _function.ErrorResponse("حساب المشترك غير موجود أو غير فعال");

        var cashAccount = await _context.Chart_Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Ch_Id == _cachedCashAccountId);

        if (cashAccount == null)
            return _function.ErrorResponse("الحساب النقدي غير محدد. يجب الاتصال بالوكيل");

        var emp = await _context.User
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == userAppUserId);

        if (emp == null)
            return _function.ErrorResponse("حساب الموضف غير محدد. يجب الاتصال بالوكيل");

        var receivable = new Receivable
        {
            Rec_Date = DateTime.Now,
            Rec_Amount = amountDue,
            FK_Receivables_Rec_SubscId = subsc,
            FK_Receivables_Rec_Cash_Account = cashAccount,
            FK_Receivables_Rec_empid = emp,
            Rec_Note = "تم الاستلام من قبل التطبيق"
        };

        await _context.Receivables.AddAsync(receivable);
        await _context.SaveChangesAsync();

        return _function.SuccessResponse("تم تسديد المبلغ. المبلغ المطلوب صفر");
    }

    public async Task<Response> CreatePaymentAsync(int subscriberId, decimal amount, string? returnUrl = null)
    {
        if (amount <= 0)
            return _function.ErrorResponse("المبلغ غير صالح");

        var subscriber = await _context.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IsValid && m.Id == subscriberId);

        if (subscriber == null)
            return _function.ErrorResponse("حساب المشترك غير موجود أو غير فعال");

        var requestId = Guid.NewGuid().ToString();
        var payment = new Payment
        {
            SubscriberId = subscriberId,
            Amount = amount,
            Currency = "IQD",
            Status = PaymentStatus.Pending,
            Type = PaymentType.OneTime,
            RequestId = requestId,
            Purpose = "PayBack",
            CreatedAt = DateTime.Now
        };

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var callbackUrl = string.IsNullOrWhiteSpace(returnUrl) ? _qiSucceedRedirect : returnUrl.Trim();
        var body = new DtoCreatePaymentObject
        {
            token = "ccd589cc-fd7c-4597-86d4-8a8b62b0573b",
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

        await _context.SaveChangesAsync();

        return _function.SuccessResponse(new
        {
            PaymentId = payment.Id,
            FormUrl = qiPayment.formUrl,
            QiPaymentId = qiPayment.qiPaymentId,
            RequestId = payment.RequestId,
            Amount = amount,
            Currency = "IQD"
        });
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

        using var httpClient = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_qiBaseUri.TrimEnd('/')}/api/Payment/Create");
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
}
