using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinWise.BudgetingAgent.ToshlApi;

public record Account(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("parent")] string? Parent,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_override")] bool? NameOverride,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("balance")] decimal Balance,
    [property: JsonPropertyName("initial_balance")] decimal InitialBalance,
    [property: JsonPropertyName("limit")] decimal? Limit,
    [property: JsonPropertyName("currency")] Currency Currency,
    [property: JsonPropertyName("main_rate")] decimal? MainRate,
    [property: JsonPropertyName("median")] Median Median,
    [property: JsonPropertyName("daily_sum_median")] Median DailySumMedian,
    [property: JsonPropertyName("avg")] Average Avg,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("modified")] string Modified,
    [property: JsonPropertyName("goal")] Goal? Goal,
    [property: JsonPropertyName("connection")] BankConnection? Connection,
    [property: JsonPropertyName("settle")] RecurrenceRule? Settle,
    [property: JsonPropertyName("billing")] RecurrenceRule? Billing,
    [property: JsonPropertyName("count")] int? Count,
    [property: JsonPropertyName("review")] int? Review,
    [property: JsonPropertyName("deleted")] bool? Deleted,
    [property: JsonPropertyName("recalculated")] bool? Recalculated,
    [property: JsonPropertyName("extra")] JsonElement? Extra
);

public record ApiClientConfig(
    [property: JsonPropertyName("baseUrl")] string BaseUrl,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("timeout")] TimeSpan? Timeout = null
);

public record Average(
    [property: JsonPropertyName("expenses")] decimal Expenses,
    [property: JsonPropertyName("incomes")] decimal Incomes
);

public record BankConnection(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("institution")] string InstitutionId,
    [property: JsonPropertyName("accounts")] IReadOnlyList<string> Accounts,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("unsorted")] int Unsorted,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("logo")] string? LogoUrl,
    [property: JsonPropertyName("refreshed")] string? LastRefreshed,
    [property: JsonPropertyName("consent_expires")] string? ConsentExpires,
    [property: JsonPropertyName("auto_refresh")] bool AutoRefresh,
    [property: JsonPropertyName("can_refresh")] bool CanRefresh,
    [property: JsonPropertyName("review")] int ReviewCount,
    [property: JsonPropertyName("deleted")] bool Deleted,
    [property: JsonPropertyName("regulated")] bool Regulated,
    [property: JsonPropertyName("partner")] bool Partner,
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("form")] IReadOnlyList<LoginFormField>? Form,
    [property: JsonPropertyName("connect_url")] string? ConnectUrl,
    [property: JsonPropertyName("reminder")] bool ReminderEnabled,
    [property: JsonPropertyName("categorisation")] bool CategorisationEnabled,
    [property: JsonPropertyName("repeats")] bool RepeatDetectionEnabled,
    [property: JsonPropertyName("transfers")] bool TransferDetectionEnabled,
    [property: JsonPropertyName("error")] BankConnectionError? Error
);

public record BankConnectionError(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("received")] string Received
);

public record BankImport(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("connection")] string Connection,
    [property: JsonPropertyName("memo")] string? Memo,
    [property: JsonPropertyName("payee")] string? Payee
);

public record BankInstitution(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("ext_id")] string ExtId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("logo")] string? LogoUrl,
    [property: JsonPropertyName("auto_refresh")] bool? AutoRefresh,
    [property: JsonPropertyName("instructions")] string? Instructions,
    [property: JsonPropertyName("provider")] string? Provider,
    [property: JsonPropertyName("flow")] string? Flow,
    [property: JsonPropertyName("form")] IReadOnlyList<FormField>? Form,
    [property: JsonPropertyName("connect_url")] string? ConnectUrl,
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("partner")] bool? Partner,
    [property: JsonPropertyName("regulated")] bool? Regulated,
    [property: JsonPropertyName("type")] string? Type
);

public record Brand(
    [property: JsonPropertyName("brand")] string BrandCode,
    [property: JsonPropertyName("name")] string BrandName,
    [property: JsonPropertyName("issuers")] List<Issuer>? Issuers
);

public record Budget(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("parent")] string? Parent,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("limit")] decimal Limit,
    [property: JsonPropertyName("limit_planned")] decimal? LimitPlanned,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("planned")] decimal Planned,
    [property: JsonPropertyName("history_amount_median")] decimal? HistoryAmountMedian,
    [property: JsonPropertyName("currency")] Currency Currency,
    [property: JsonPropertyName("main_rate")] decimal? MainRate,
    [property: JsonPropertyName("fixed")] bool Fixed,
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string To,
    [property: JsonPropertyName("rollover")] bool Rollover,
    [property: JsonPropertyName("rollover_override")] bool? RolloverOverride,
    [property: JsonPropertyName("rollover_amount")] decimal RolloverAmount,
    [property: JsonPropertyName("rollover_amount_planned")] decimal? RolloverAmountPlanned,
    [property: JsonPropertyName("modified")] string Modified,
    [property: JsonPropertyName("recurrence")] Recurrence? Recurrence,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("percent")] decimal? Percent,
    [property: JsonPropertyName("delta")] decimal? Delta,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("!tags")] IReadOnlyList<string>? TagsExcluded,
    [property: JsonPropertyName("categories")] IReadOnlyList<string>? Categories,
    [property: JsonPropertyName("!categories")] IReadOnlyList<string>? CategoriesExcluded,
    [property: JsonPropertyName("accounts")] IReadOnlyList<string>? Accounts,
    [property: JsonPropertyName("!accounts")] IReadOnlyList<string>? AccountsExcluded,
    [property: JsonPropertyName("deleted")] bool? Deleted,
    [property: JsonPropertyName("recalculated")] bool? Recalculated,
    [property: JsonPropertyName("extra")] JsonElement? Extra,
    [property: JsonPropertyName("problem")] BudgetProblem? Problem,
    [property: JsonPropertyName("deleted_accounts")] IReadOnlyList<string>? DeletedAccounts,
    [property: JsonPropertyName("deleted_tags")] IReadOnlyList<string>? DeletedTags,
    [property: JsonPropertyName("deleted_categories")] IReadOnlyList<string>? DeletedCategories
);

public record BudgetProblem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("description")] string Description
);

public record CacheConfig(
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("ttl")] int Ttl // Time to live in seconds
);

public record Category(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_override")] bool? NameOverride,
    [property: JsonPropertyName("modified")] string Modified,
    [property: JsonPropertyName("type")] string Type, // expense, income, or system
    [property: JsonPropertyName("deleted")] bool Deleted,
    [property: JsonPropertyName("counts")] CategoryCounts? Counts,
    [property: JsonPropertyName("extra")] JsonElement? Extra
);

public record CategoryCounts(
    [property: JsonPropertyName("entries")] int? Entries,
    [property: JsonPropertyName("income_entries")] int? IncomeEntries,
    [property: JsonPropertyName("expense_entries")] int? ExpenseEntries,
    [property: JsonPropertyName("tags_used_with_category")] int? TagsUsed,
    [property: JsonPropertyName("income_tags_used_with_category")] int? IncomeTagsUsed,
    [property: JsonPropertyName("expense_tags_used_with_category")] int? ExpenseTagsUsed,
    [property: JsonPropertyName("tags")] int? Tags,
    [property: JsonPropertyName("income_tags")] int? IncomeTags,
    [property: JsonPropertyName("expense_tags")] int? ExpenseTags,
    [property: JsonPropertyName("budgets")] int? Budgets
);

public record SupportedCurrency(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("precision")] int Precision,
    [property: JsonPropertyName("modified")] string Modified,
    [property: JsonPropertyName("type")] string Type // fiat, crypto, commodity, or deprecated
);

public record Currency(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("rate")] decimal Rate,
    [property: JsonPropertyName("fixed")] bool Fixed
);

public record CurrencySettings(
    [property: JsonPropertyName("main")] string Main,
    [property: JsonPropertyName("update")] string? UpdateType = null,
    [property: JsonPropertyName("update_accounts")] bool? UpdateAccounts = null,
    [property: JsonPropertyName("custom_exchange_rate")] decimal? CustomExchangeRate = null,
    [property: JsonPropertyName("custom")] Currency? Custom = null,
    [property: JsonPropertyName("reference_currency")] string? ReferenceCurrency = null
);

public record Entry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] Currency Currency,
    [property: JsonPropertyName("main_rate")] decimal? MainRate,
    [property: JsonPropertyName("fixed")] bool Fixed,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("desc")] string? Description,
    [property: JsonPropertyName("account")] string Account,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("location")] Location? Location,
    [property: JsonPropertyName("modified")] string Modified,
    [property: JsonPropertyName("repeat")] Repeat? Repeat,
    [property: JsonPropertyName("transaction")] Transaction? Transaction,
    [property: JsonPropertyName("images")] IReadOnlyList<EntryImage>? Images,
    [property: JsonPropertyName("reminders")] IReadOnlyList<Reminder>? Reminders,
    [property: JsonPropertyName("completed")] bool? Completed,
    [property: JsonPropertyName("created")] string? Created,
    [property: JsonPropertyName("import")] BankImport? Import,
    [property: JsonPropertyName("payee")] string? Payee,
    [property: JsonPropertyName("memo")] string? Memo,
    [property: JsonPropertyName("pending")] bool? Pending,
    [property: JsonPropertyName("review")] Review? Review,
    [property: JsonPropertyName("settle")] Settle? Settle,
    [property: JsonPropertyName("split")] Split? Split,
    [property: JsonPropertyName("extra")] JsonElement? Extra,
    [property: JsonPropertyName("deleted")] bool? Deleted
);

public record EntryImage(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("path")] string? Path,
    [property: JsonPropertyName("filename")] string? Filename,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("status")] string? Status
);

public record EntrySum(
    [property: JsonPropertyName("day")] string Day,
    [property: JsonPropertyName("modified")] string Modified,
    [property: JsonPropertyName("expenses")] EntrySumExpenses? Expenses = null,
    [property: JsonPropertyName("incomes")] EntrySumIncomes? Incomes = null
);

public record EntrySumExpenses(
    [property: JsonPropertyName("sum")] decimal Sum,
    [property: JsonPropertyName("count")] int Count
);

public record EntrySumIncomes(
    [property: JsonPropertyName("sum")] decimal Sum,
    [property: JsonPropertyName("count")] int Count
);

public record Expenses(
    [property: JsonPropertyName("sum")] decimal Sum,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("daily_median")] decimal DailyMedian,
    [property: JsonPropertyName("sum_planned")] decimal? SumPlanned = null,
    [property: JsonPropertyName("all_time_avg")] decimal? AllTimeAvg = null
);

public record FormField(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("options")] IReadOnlyList<string>? Options
);

public record Goal(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("start")] string Start,
    [property: JsonPropertyName("end")] string End
);

public record Image(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("status")] string Status
);

public record Incomes(
    [property: JsonPropertyName("sum")] decimal Sum,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("daily_median")] decimal DailyMedian,
    [property: JsonPropertyName("sum_planned")] decimal? SumPlanned = null,
    [property: JsonPropertyName("all_time_avg")] decimal? AllTimeAvg = null
);

public record Issuer(
    [property: JsonPropertyName("issuer")] string IssuerCode,
    [property: JsonPropertyName("name")] string IssuerName
);

public record Limits(
    [property: JsonPropertyName("accounts")] bool Accounts,
    [property: JsonPropertyName("budgets")] bool Budgets,
    [property: JsonPropertyName("images")] bool Images
);

public record Location(
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("venue_id")] string? VenueId = null,
    [property: JsonPropertyName("latitude")] decimal? Latitude = null,
    [property: JsonPropertyName("longitude")] decimal? Longitude = null
);

public record LoginFormField(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("options")] IReadOnlyList<string>? Options,
    [property: JsonPropertyName("value")] string? Value
);

public record Median(
    [property: JsonPropertyName("expenses")] decimal Expenses,
    [property: JsonPropertyName("incomes")] decimal Incomes
);

public record Payment(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("period")] string Period,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("discount")] decimal Discount,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("modified")] string Modified,
    [property: JsonPropertyName("refund")] bool Refund,
    [property: JsonPropertyName("subscription")] bool Subscription,
    [property: JsonPropertyName("promo")] string? Promo = null,
    [property: JsonPropertyName("vat")] VatInfo? Vat = null,
    [property: JsonPropertyName("receipt")] ReceiptInfo? Receipt = null,
    [property: JsonPropertyName("perks")] List<Perk>? Perks = null,
    [property: JsonPropertyName("address")] ShippingAddress? Address = null,
    [property: JsonPropertyName("trial")] bool? Trial = null,
    [property: JsonPropertyName("redirect")] string? Redirect = null,
    [property: JsonPropertyName("refunduble")] bool? Refundable = null,
    [property: JsonPropertyName("make_refund")] bool? MakeRefund = null,
    [property: JsonPropertyName("cancel_subscription")] bool? CancelSubscription = null,
    [property: JsonPropertyName("brand")] string? Brand = null,
    [property: JsonPropertyName("issuer")] string? Issuer = null,
    [property: JsonPropertyName("method")] string? Method = null,
    [property: JsonPropertyName("level")] string? Level = null
);

public record PaymentType(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("brands")] List<Brand>? Brands
);

public record Perk(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("model")] string? Model = null
);

public record Planning(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("modified")] string Modified
);

public record PlanningOverview(
    [property: JsonPropertyName("avg")] PlanningAverages Avg,
    [property: JsonPropertyName("ranges")] PlanningRanges Ranges,
    [property: JsonPropertyName("planning")] IReadOnlyList<MonthlyPlan> Planning
);

public record PlanningAverages(
    [property: JsonPropertyName("expenses")] decimal Expenses,
    [property: JsonPropertyName("incomes")] decimal Incomes,
    [property: JsonPropertyName("balance")] decimal Balance,
    [property: JsonPropertyName("networth")] decimal NetWorth
);

public record PlanningRanges(
    [property: JsonPropertyName("expenses")] MinMax Expenses,
    [property: JsonPropertyName("incomes")] MinMax Incomes,
    [property: JsonPropertyName("balance")] MinMax Balance,
    [property: JsonPropertyName("networth")] MinMax NetWorth
);

public record MinMax(
    [property: JsonPropertyName("min")] decimal Min,
    [property: JsonPropertyName("max")] decimal Max
);

public record MonthlyPlan(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string To,
    [property: JsonPropertyName("expenses")] PlanningStats Expenses,
    [property: JsonPropertyName("incomes")] PlanningStats Incomes,
    [property: JsonPropertyName("balance")] PlanningStats Balance,
    [property: JsonPropertyName("estimated")] PlanningStats Estimated
);

public record PlanningStats(
    [property: JsonPropertyName("sum")] decimal Sum,
    [property: JsonPropertyName("planned")] decimal Planned,
    [property: JsonPropertyName("predicted")] decimal Predicted
);

public record ProPayment(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("next")] string Next
);

public record ProStatus(
    [property: JsonPropertyName("since")] string Since,
    [property: JsonPropertyName("until")] string Until,
    [property: JsonPropertyName("payment")] ProPayment Payment
);

public record ReceiptInfo(
    [property: JsonPropertyName("data")] string Data,
    [property: JsonPropertyName("signature")] string? Signature = null
);

public record Recurrence(
    [property: JsonPropertyName("frequency")] string Frequency, // one-time, daily, weekly, monthly, yearly
    [property: JsonPropertyName("interval")] int Interval,
    [property: JsonPropertyName("start")] string Start,
    [property: JsonPropertyName("end")] string End,
    [property: JsonPropertyName("byday")] string? ByDay,
    [property: JsonPropertyName("bymonthday")] string? ByMonthDay,
    [property: JsonPropertyName("bysetpos")] string? BySetPos,
    [property: JsonPropertyName("iteration")] int? Iteration
);

public record RecurrenceRule(
    [property: JsonPropertyName("byday")] string? ByDay,
    [property: JsonPropertyName("bymonthday")] string? ByMonthDay,
    [property: JsonPropertyName("bysetpos")] string? BySetPos
);

public record Reminder(
    [property: JsonPropertyName("period")] string Period,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("at")] string At
);

public record Repeat(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("frequency")] string Frequency,
    [property: JsonPropertyName("interval")] int Interval,
    [property: JsonPropertyName("start")] string Start,
    [property: JsonPropertyName("end")] string? End,
    [property: JsonPropertyName("count")] int? Count,
    [property: JsonPropertyName("byday")] string? ByDay,
    [property: JsonPropertyName("bymonthday")] string? ByMonthDay,
    [property: JsonPropertyName("bysetpos")] string? BySetPos,
    [property: JsonPropertyName("iteration")] int? Iteration,
    [property: JsonPropertyName("template")] bool? Template,
    [property: JsonPropertyName("entries")] IReadOnlyList<string>? Entries,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("status")] string? Status
);

public record ResourceParams(
    [property: JsonPropertyName("parameters")] Dictionary<string, string> Parameters
);

public record Review(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("completed")] bool Completed
);

public record ServerConfig(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version
);

public record Settle(
    [property: JsonPropertyName("id")] string Id
);

public record ShippingAddress(
    [property: JsonPropertyName("name")] string RecipientName,
    [property: JsonPropertyName("address")] string StreetAddress
);

public record Split(
    [property: JsonPropertyName("parent")] string Parent,
    [property: JsonPropertyName("children")] IReadOnlyList<string> Children,
    [property: JsonPropertyName("completed")] bool? Completed
);

public record Summary(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string To,
    [property: JsonPropertyName("expenses")] Expenses Expenses,
    [property: JsonPropertyName("incomes")] Incomes Incomes,
    [property: JsonPropertyName("budget")] Budget Budget,
    [property: JsonPropertyName("left")] decimal Left
);

public record Tag(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("modified")] string Modified
);

public record TimelineItem(
    [property: JsonPropertyName("day")] string Day,
    [property: JsonPropertyName("sum")] decimal Sum,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("entries")] Entry[] Entries
);

public record ToolInput(
    [property: JsonPropertyName("properties")] Dictionary<string, object> Properties
);

public record ToolOutput(
    [property: JsonPropertyName("properties")] Dictionary<string, object> Properties
);

public record Transaction(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("account")] string Account,
    [property: JsonPropertyName("currency")] Currency Currency,
    [property: JsonPropertyName("main_rate")] decimal? MainRate,
    [property: JsonPropertyName("fixed")] bool Fixed
);

public record User(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("joined")] string Joined,
    [property: JsonPropertyName("modified")] string Modified,
    [property: JsonPropertyName("pro")] ProStatus? Pro,
    [property: JsonPropertyName("currency")] UserCurrencySettings Currency,
    [property: JsonPropertyName("start_day")] int StartDay,
    [property: JsonPropertyName("notifications")] int Notifications,
    [property: JsonPropertyName("social")] IReadOnlyList<string>? Social,
    [property: JsonPropertyName("steps")] IReadOnlyList<string>? Steps,
    [property: JsonPropertyName("limits")] Limits Limits
);

public record UserCurrencySettings(
    [property: JsonPropertyName("main")] string Main,
    [property: JsonPropertyName("recent")] IReadOnlyList<RecentCurrency>? Recent
);

public record RecentCurrency(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("rate")] decimal Rate,
    [property: JsonPropertyName("fixed")] bool Fixed,
    [property: JsonPropertyName("reference_currency")] string ReferenceCurrency
);

public record VatInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("post")] string Post,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("vat")] string VatNumber
);
