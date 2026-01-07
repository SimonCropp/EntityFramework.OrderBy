static class PropertyInfoHelper
{
    /// <summary>
    /// Gets the CLR PropertyInfo for a property from the EF model metadata.
    /// </summary>
    internal static PropertyInfo GetPropertyInfo(IEntityType entityType, string propertyName)
    {
        var efProperty = entityType.FindProperty(propertyName);
        if (efProperty == null)
        {
            throw new($"Property '{propertyName}' not found on entity type '{entityType.Name}'");
        }

        var propertyInfo = efProperty.PropertyInfo;
        if (propertyInfo == null)
        {
            throw new($"Property '{propertyName}' on entity type '{entityType.Name}' does not have a CLR property");
        }

        return propertyInfo;
    }
}
