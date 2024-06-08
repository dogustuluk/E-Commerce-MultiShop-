namespace MultiShop.Catalog.Utilities
{
    public static class Pluralizer
    {
        public static string GetPluralForm(string typeName)
        {
            if (typeName.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                return typeName.Substring(0, typeName.Length - 1) + "ies";
            }
            else if (typeName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return typeName + "es";
            }
            else
            {
                return typeName + "s";
            }
        }

    }
}
