using TMS.Domain.Enums;

namespace TMS.Application.Interfaces;

public interface ITransactionPropertyHelper
{
    /// <summary>
    /// Gets the <see cref="TransactionPropertyName"/> corresponding to the provided property name.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <returns>The <see cref="TransactionPropertyName"/> if found; otherwise, null.</returns>
    public TransactionPropertyName? GetProperty(string? name);

    /// <summary>
    /// Gets the list of <see cref="TransactionPropertyName"/> corresponding 
    /// to the provided property names.
    /// </summary>
    /// <param name="names">The names of the properties.</param>
    /// <returns>The list of <see cref="TransactionPropertyName"/>.</returns>
    public List<TransactionPropertyName> GetPropertiesTypes(IEnumerable<string> names);

    /// <summary>
    /// Gets the database column names corresponding to the provided
    /// <see cref="TransactionPropertyName"/> properties.
    /// </summary>
    /// <param name="properties">The properties for which to retrieve database column names.</param>
    /// <returns>The list of database column names.</returns>
    public List<string> GetDatabaseColumnNames(IEnumerable<TransactionPropertyName> properties);

    /// <summary>
    /// Gets the database column name for the provided <see cref="TransactionPropertyName"/>.
    /// </summary>
    /// <param name="property">The property for which to retrieve the database column name.</param>
    /// <returns>The database column name if available; otherwise, null.</returns>
    public string? GetDatabaseColumnName(TransactionPropertyName property);

    /// <summary>
    /// Gets the displayed name for the provided <see cref="TransactionPropertyName"/>.
    /// </summary>
    /// <param name="property">The property for which to retrieve the displayed name.</param>
    /// <returns>The displayed name if available; otherwise, null.</returns>
    public string? GetDisplayedName(TransactionPropertyName property);

    /// <summary>
    /// Checks if the provided name is a database column name.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is a database column name; otherwise, false.</returns>
    public bool IsDatabaseColumnName(string name);

    /// <summary>
    /// Checks if the provided name is a displayed name.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is a displayed name; otherwise, false.</returns>
    public bool IsDisplayedName(string name);
}
