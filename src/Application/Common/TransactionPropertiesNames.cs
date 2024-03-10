namespace TMS.Application.Common;

public static class TransactionPropertiesNames
{
    public static PropertyNames TransactionId => new()
    {
        NormalizedName = "transactionid",
        DataBaseName = "Transactions.TransactionId",
        DisplayedName = "tranzaction_id"
    };

    public static PropertyNames TransactionIdShort => new()
    {
        NormalizedName = "id",
        DataBaseName = "Transactions.TransactionId",
        DisplayedName = "tranzaction_id"
    };
    public static PropertyNames Name => new()
    {
        NormalizedName = "name",
        DataBaseName = "Clients.Name",
        DisplayedName = "name"
    };
    public static PropertyNames Email => new()
    {
        NormalizedName = "email",
        DataBaseName = "Clients.Email",
        DisplayedName = "email"
    };
    public static PropertyNames Amount => new()
    {
        NormalizedName = "amount",
        DataBaseName = "Transactions.Amount",
        DisplayedName = "amount"
    };
    public static PropertyNames TransactionDate => new()
    {
        NormalizedName = "transactiondate",
        DataBaseName = "Transactions.TransactionDate",
        DisplayedName = "tranzaction_date"
    };
    public static PropertyNames Offset => new()
    {
        NormalizedName = "offset",
        DisplayedName = "offset"
    };
    public static PropertyNames Latitude => new()
    {
        NormalizedName = "latitude",
        DataBaseName = "Clients.Latitude",
        DisplayedName = "client_location_latitude"
    };
    public static PropertyNames Longitude => new()
    {
        NormalizedName = "longitude",
        DataBaseName = "Clients.Longitude",
        DisplayedName = "client_location_longitude"
    };
    private static Dictionary<string, PropertyNames> Properties => new()
        {
            { TransactionId.NormalizedName, TransactionId },
            { TransactionIdShort.NormalizedName, TransactionIdShort },
            { Name.NormalizedName, Name },
            { Email.NormalizedName, Email },
            { Amount.NormalizedName, Amount },
            { TransactionDate.NormalizedName, TransactionDate },
            { Offset.NormalizedName, Offset },
            { Latitude.NormalizedName, Latitude },
            { Longitude.NormalizedName, Longitude }
        };

    public static PropertyNames? GetByNormalizedName(string name)
    {
        name = name.ToLower().Replace("_", "");
        return Properties[name];
    }

    public static List<PropertyNames> GetProperiesByNames(IEnumerable<string> names)
    {
        List<PropertyNames> properties = [];
        foreach (var name in names)
        {
            var prop = GetByNormalizedName(name);
            if (prop != null)
                properties.Add(prop);
        }
        return properties;
    }
}
