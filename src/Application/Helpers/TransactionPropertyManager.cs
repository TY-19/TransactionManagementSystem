using TMS.Application.Interfaces;
using TMS.Domain.Enums;

namespace TMS.Application.Helpers;

public class TransactionPropertyManager : ITransactionPropertyManager
{
    private readonly Dictionary<TransactionPropertyName, string> normalizedNames = new()
    {
        { TransactionPropertyName.TransactionId, "transactionid" },
        { TransactionPropertyName.Name, "name" },
        { TransactionPropertyName.Email, "email" },
        { TransactionPropertyName.Amount, "amount" },
        { TransactionPropertyName.TransactionDate, "transactiondate" },
        { TransactionPropertyName.Offset, "offset" },
        { TransactionPropertyName.Latitude, "latitude" },
        { TransactionPropertyName.Longitude, "longitude" }
    };

    private readonly Dictionary<TransactionPropertyName, string?> dataBaseColumnNames = new()
    {
        { TransactionPropertyName.TransactionId, "Transactions.TransactionId" },
        { TransactionPropertyName.Name, "Clients.Name" },
        { TransactionPropertyName.Email, "Clients.Email" },
        { TransactionPropertyName.Amount, "Transactions.Amount" },
        { TransactionPropertyName.TransactionDate, "Transactions.TransactionDate" },
        { TransactionPropertyName.Offset, "calculateOffset" },
        { TransactionPropertyName.Latitude, "Clients.Latitude" },
        { TransactionPropertyName.Longitude, "Clients.Longitude" }
    };

    private readonly Dictionary<TransactionPropertyName, string> displayedNames = new()
    {
        { TransactionPropertyName.TransactionId, "transaction_id" },
        { TransactionPropertyName.Name, "name" },
        { TransactionPropertyName.Email, "email" },
        { TransactionPropertyName.Amount, "amount" },
        { TransactionPropertyName.TransactionDate, "transaction_date" },
        { TransactionPropertyName.Offset, "offset" },
        { TransactionPropertyName.Latitude, "client_location_latitude" },
        { TransactionPropertyName.Longitude, "client_location_longitude" }
    };

    private readonly Dictionary<string, TransactionPropertyName> aliases = new()
    {
        { "id", TransactionPropertyName.TransactionId }
    };

    public TransactionPropertyName? GetProperty(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        name = name.ToLower().Replace("_", "");

        if (normalizedNames.ContainsValue(name))
            return normalizedNames.First(n => n.Value == name).Key;

        if (aliases.TryGetValue(name, out var alias))
            return alias;

        if (dataBaseColumnNames.ContainsValue(name))
            return dataBaseColumnNames.First(n => n.Value == name).Key;

        if (displayedNames.ContainsValue(name))
            return displayedNames.First(n => n.Value == name).Key;

        return null;
    }
    public List<TransactionPropertyName> GetPropertiesTypes(IEnumerable<string> names)
    {
        List<TransactionPropertyName> properties = [];
        foreach (var name in names)
        {
            var prop = GetProperty(name);
            if (prop != null)
                properties.Add(prop.Value);
        }
        return properties;
    }
    public List<string> GetDatabaseColumnNames(IEnumerable<TransactionPropertyName> properties)
    {
        List<string> dbNames = [];
        foreach (var prop in properties)
        {
            var dbName = dataBaseColumnNames[prop];
            if (dbName != null)
                dbNames.Add(dbName);
        }
        return dbNames;
    }

    public string? GetNormalizedName(TransactionPropertyName property)
        => normalizedNames[property];

    public string? GetDatabaseColumnName(TransactionPropertyName property)
        => dataBaseColumnNames[property];

    public string? GetDisplayedName(TransactionPropertyName property)
        => displayedNames[property];
}
