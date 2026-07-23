using IBSMobile.Models;
using Microsoft.EntityFrameworkCore;
using IBSMobile.Services;
using IBSMobile.Models.Profiles;

namespace IBSMobile.Data;

/// <summary>
/// Entity Framework context for the application. Apply schema changes with:
/// <c>dotnet ef database update</c> (run from the project directory; requires the EF CLI tools).
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // affiliate
    public DbSet<MainAffiliate> MainAffiliates => Set<MainAffiliate>();
    public DbSet<RefreshTokens> RefreshTokens => Set<RefreshTokens>();
    public DbSet<SubAffiliate> SubAffiliates => Set<SubAffiliate>();
    //

    // profiles
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileCost> ProfileCosts => Set<ProfileCost>();
    public DbSet<ProfileAppCost> ProfileAppCosts => Set<ProfileAppCost>();
    public DbSet<ProfileCostCustomizer> ProfileCostCustomizers => Set<ProfileCostCustomizer>();
    public DbSet<ProfileFirstActivationCost> ProfileFirstActivationCosts => Set<ProfileFirstActivationCost>();
    public DbSet<ProfileGetCostBySubAffiliate> ProfileGetCostBySubAffiliates => Set<ProfileGetCostBySubAffiliate>();
    //

    // settings
    public DbSet<Chart_Account> Chart_Accounts => Set<Chart_Account>();
    public DbSet<ApiServerSetting> ApiServerSettings => Set<ApiServerSetting>();
    //

    // subscriber
    public DbSet<Get_Tansaction_History_Subscriber> Get_Tansaction_History_Subscribers => Set<Get_Tansaction_History_Subscriber>();
    public DbSet<GetSecondActivationDay> GetSecondActivationDays => Set<GetSecondActivationDay>();
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<SubscriberAccessToken> SubscriberAccessTokens => Set<SubscriberAccessToken>();
    public DbSet<SubscriberApp> SubscriberApp => Set<SubscriberApp>();
    public DbSet<SubscriberDiscount> SubscriberDiscounts => Set<SubscriberDiscount>();
    public DbSet<SubscriberGroup> SubscriberGroups => Set<SubscriberGroup>();
    public DbSet<SubscribersCredit> SubscribersCredits => Set<SubscribersCredit>();
    //

    public DbSet<Activation_User> Activation_Users => Set<Activation_User>();
    public DbSet<Receivable> Receivables => Set<Receivable>();
    public DbSet<User> User => Set<User>();
    public DbSet<UserApp> UserApp => Set<UserApp>();
    public DbSet<Payment> Payments => Set<Payment>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Get_Tansaction_History_Subscriber>().HasNoKey();

        modelBuilder.Entity<SubscribersCredit>()
            .HasKey(x => x.SubscId);

        modelBuilder.Entity<Subscriber>(entity =>
        {
            entity.Property<int>("MainAffiliate");
            entity.Property<int>("SubAffiliate");

            entity.HasOne(s => s.FK_Subscribers_MainAffiliate)
                .WithMany(a => a.Subscribers)
                .HasForeignKey("MainAffiliate")
                .HasPrincipalKey(a => a.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.FK_Subscribers_SubAffiliate)
                .WithMany()
                .HasForeignKey("SubAffiliate")
                .HasPrincipalKey(a => a.Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.FK_Subscribers_Employee)
                .WithMany(u => u.Subscribers)
                .HasForeignKey("Employee")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.AccountCh_Id)
                .WithMany(c => c.Subscribers)
                .HasForeignKey("AccountId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasOne(u => u.FK_UserCashAccount)
                .WithMany(c => c.users)
                .HasForeignKey("UserCashAccount")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.FK_UserSubscAccount)
                .WithMany(c => c.users2)
                .HasForeignKey("UserSubscAccount")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.FK_UserAgentsAccount)
                .WithMany(c => c.users3)
                .HasForeignKey("UserAgentsAccount")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubAffiliate>(entity =>
        {
            // DB column is MainAffiliate, not MainAffiliateId (EF convention default).
            entity.HasOne<MainAffiliate>()
                .WithMany(m => m.SubAffiliates)
                .HasForeignKey(s => s.MainAffiliate)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Receivable>(entity =>
        {
            // SQL Server trigger blocks EF OUTPUT clause on INSERT/UPDATE.
            entity.ToTable(tb => tb.HasTrigger("ReceivablesTrigger"));
        });

        modelBuilder.Entity<Activation_User>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("Activation_UsersTrigger"));
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            // DB column TransactionType is nvarchar storing enum numeric values as text ("1").
            entity.Property(p => p.Type)
                .HasColumnName("TransactionType")
                .HasConversion(
                    v => ((int)v).ToString(),
                    v => ParsePaymentType(v));
        });

        base.OnModelCreating(modelBuilder);
    }

    private static PaymentType ParsePaymentType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return PaymentType.OneTime;

        if (Enum.TryParse<PaymentType>(value, true, out var byName))
            return byName;

        return int.TryParse(value, out var numeric)
            ? (PaymentType)numeric
            : PaymentType.OneTime;
    }

}
