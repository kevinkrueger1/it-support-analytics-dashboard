using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// A local SQLite file keeps this prototype completely free to run.
var databasePath = Path.Combine(app.Environment.ContentRootPath, "tickets.db");
var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
InitializeDatabase(connectionString);

// Serve wwwroot/index.html at http://localhost:5000/.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/summary", () =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var totalsCommand = connection.CreateCommand();
    totalsCommand.CommandText = """
        SELECT COUNT(*),
               COALESCE(SUM(CASE WHEN Status = 'Open' THEN 1 ELSE 0 END), 0),
               COALESCE(SUM(CASE WHEN Status = 'Resolved' THEN 1 ELSE 0 END), 0)
        FROM Tickets;
        """;
    using var totalsReader = totalsCommand.ExecuteReader();
    totalsReader.Read();
    var totalTickets = totalsReader.GetInt64(0);
    var openTickets = totalsReader.GetInt64(1);
    var resolvedTickets = totalsReader.GetInt64(2);
    totalsReader.Close();

    using var categoryCommand = connection.CreateCommand();
    categoryCommand.CommandText = """
        SELECT Category, COUNT(*)
        FROM Tickets
        GROUP BY Category
        ORDER BY COUNT(*) DESC;
        """;
    var categories = new List<CategoryCount>();
    using (var reader = categoryCommand.ExecuteReader())
    {
        while (reader.Read())
            categories.Add(new CategoryCount(reader.GetString(0), reader.GetInt64(1)));
    }

    using var statusCommand = connection.CreateCommand();
    statusCommand.CommandText = """
        SELECT Status, COUNT(*)
        FROM Tickets
        GROUP BY Status
        ORDER BY Status;
        """;
    var statuses = new List<StatusCount>();
    using (var reader = statusCommand.ExecuteReader())
    {
        while (reader.Read())
            statuses.Add(new StatusCount(reader.GetString(0), reader.GetInt64(1)));
    }

    return Results.Ok(new { totalTickets, openTickets, resolvedTickets, categories, statuses });
});

// Optional query filters; parameterized SQL prevents SQL injection.
app.MapGet("/api/tickets", (string? category, string? status) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT Id, Title, Category, Status, CreatedAt
        FROM Tickets
        WHERE (@category = '' OR Category = @category)
          AND (@status = '' OR Status = @status)
        ORDER BY CreatedAt DESC
        LIMIT 100;
        """;
    command.Parameters.AddWithValue("@category", category ?? "");
    command.Parameters.AddWithValue("@status", status ?? "");

    var tickets = new List<Ticket>();
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        tickets.Add(new Ticket(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4)));
    }
    return Results.Ok(tickets);
});

app.Run();

static void InitializeDatabase(string connectionString)
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var createCommand = connection.CreateCommand();
    createCommand.CommandText = """
        CREATE TABLE IF NOT EXISTS Tickets (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            Category TEXT NOT NULL,
            Status TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        """;
    createCommand.ExecuteNonQuery();

    using var countCommand = connection.CreateCommand();
    countCommand.CommandText = "SELECT COUNT(*) FROM Tickets;";
    if (Convert.ToInt64(countCommand.ExecuteScalar()) > 0)
        return;

    // Synthetic sample data only — no actual employee or company information.
    var examples = new (string Title, string Category)[]
    {
        ("Unable to sign in to workstation", "Active Directory"),
        ("Account locked after password attempts", "Active Directory"),
        ("Need access to a shared folder", "Fileservice"),
        ("Shared drive permissions not working", "Fileservice"),
        ("Email messages not syncing", "O365"),
        ("Cannot access Teams meeting", "O365"),
        ("Application crashes during startup", "Software"),
        ("Request to install approved software", "Software"),
        ("Laptop setup for new employee", "Computer-Services"),
        ("Printer not connecting to device", "Computer-Services"),
        ("Replacement device request", "EOL"),
        ("Older laptop needs retirement", "EOL"),
        ("General IT assistance request", "Support general"),
        ("Question about support procedures", "Support general")
    };
    var statuses = new[] { "Open", "In Progress", "Resolved" };
    var random = new Random(42); // repeatable sample distribution
    using var transaction = connection.BeginTransaction();

    for (var i = 0; i < 150; i++)
    {
        var example = examples[random.Next(examples.Length)];
        var status = statuses[random.Next(statuses.Length)];
        var createdAt = DateTime.UtcNow.AddDays(-random.Next(0, 31))
            .AddMinutes(-random.Next(0, 1440)).ToString("O");

        using var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
        insertCommand.CommandText = """
            INSERT INTO Tickets (Title, Category, Status, CreatedAt)
            VALUES (@title, @category, @status, @createdAt);
            """;
        insertCommand.Parameters.AddWithValue("@title", example.Title);
        insertCommand.Parameters.AddWithValue("@category", example.Category);
        insertCommand.Parameters.AddWithValue("@status", status);
        insertCommand.Parameters.AddWithValue("@createdAt", createdAt);
        insertCommand.ExecuteNonQuery();
    }
    transaction.Commit();
}

record CategoryCount(string Category, long Count);
record StatusCount(string Status, long Count);
record Ticket(long Id, string Title, string Category, string Status, string CreatedAt);
