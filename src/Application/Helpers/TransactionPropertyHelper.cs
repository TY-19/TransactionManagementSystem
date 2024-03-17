using TMS.Application.Interfaces;
using TMS.Domain.Enums;

namespace TMS.Application.Helpers;

public class TransactionPropertyHelper : ITransactionPropertyHelper
{
    private readonly Dictionary<TransactionPropertyName, string> _normalizedNames = new()
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
    private readonly Dictionary<TransactionPropertyName, string?> _dataBaseColumnNames = new()
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
    private readonly Dictionary<TransactionPropertyName, string> _displayedNames = new()
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
    private readonly Dictionary<string, TransactionPropertyName> _aliases = new()
    {
        { "id", TransactionPropertyName.TransactionId }
    };

    /// <inheritdoc cref="ITransactionPropertyHelper.GetProperty(string?)"/>
    public TransactionPropertyName? GetProperty(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        name = name.ToLower().Replace("_", "").Replace(" ", "");
        if (_normalizedNames.ContainsValue(name))
        {
            return _normalizedNames.First(n => n.Value == name).Key;
        }
        if (_aliases.TryGetValue(name, out TransactionPropertyName alias))
        {
            return alias;
        }
        if (IsDatabaseColumnName(name))
        {
            return _dataBaseColumnNames.First(n => n.Value == name).Key;
        }
        if (IsDisplayedName(name))
        {
            return _displayedNames.First(n => n.Value == name).Key;
        }
        return null;
    }
    /// <inheritdoc cref="ITransactionPropertyHelper.GetPropertiesTypes(IEnumerable{string})"/>
    public List<TransactionPropertyName> GetPropertiesTypes(IEnumerable<string> names)
    {
        List<TransactionPropertyName> properties = [];
        foreach (string name in names)
        {
            TransactionPropertyName? prop = GetProperty(name);
            if (prop != null)
            {
                properties.Add(prop.Value);
            }
        }
        return properties;
    }
    /// <inheritdoc cref="ITransactionPropertyHelper.GetDatabaseColumnNames(IEnumerable{TransactionPropertyName})"/>
    public List<string> GetDatabaseColumnNames(IEnumerable<TransactionPropertyName> properties)
    {
        List<string> dbNames = [];
        foreach (TransactionPropertyName prop in properties)
        {
            string? dbName = _dataBaseColumnNames[prop];
            if (dbName != null)
            {
                dbNames.Add(dbName);
            }
        }
        return dbNames;
    }
    /// <inheritdoc cref="ITransactionPropertyHelper.GetDatabaseColumnName(TransactionPropertyName)"/>
    public string? GetDatabaseColumnName(TransactionPropertyName property)
    {
        return _dataBaseColumnNames[property];
    }
    /// <inheritdoc cref="ITransactionPropertyHelper.GetDisplayedName(TransactionPropertyName)"/>
    public string? GetDisplayedName(TransactionPropertyName property)
    {
        return _displayedNames[property];
    }
    /// <inheritdoc cref="ITransactionPropertyHelper.IsDatabaseColumnName(string)/>
    public bool IsDatabaseColumnName(string name)
    {
        return _dataBaseColumnNames.ContainsValue(name);
    }
    /// <inheritdoc cref="ITransactionPropertyHelper.IsDisplayedName(string)"/>
    public bool IsDisplayedName(string name)
    {
        return _displayedNames.ContainsValue(name);
    }
}
