using TMS.Domain.Enums;

namespace TMS.Application.Interfaces;

public interface ITransactionPropertyManager
{
    TransactionPropertyName? GetProperty(string name);
    List<TransactionPropertyName> GetPropertiesTypes(IEnumerable<string> names);
    List<string> GetDatabaseColumnNames(IEnumerable<TransactionPropertyName> properties);
    string? GetNormalizedName(TransactionPropertyName property);
    string? GetDatabaseColumnName(TransactionPropertyName property);
    string? GetDisplayedName(TransactionPropertyName property);
}
