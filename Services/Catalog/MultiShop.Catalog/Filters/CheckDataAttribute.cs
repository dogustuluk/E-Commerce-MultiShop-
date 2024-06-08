using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Driver;
using MultiShop.Catalog.Enums;
using MultiShop.Catalog.Settings;
using MultiShop.Catalog.Utilities;
using System.Reflection;

//public enum CheckType
//{
//    Existing,
//    Unique
//}

namespace MultiShop.Catalog.Filters
{
    public class CheckDataAttribute<T> : Attribute, IAsyncActionFilter
    {
        private readonly PropertyInfo[] _propertyInfos;
        private readonly CheckType[] _checkTypes;
        private readonly IMongoCollection<T> _collection;

        public CheckDataAttribute(Type entityType, string[] propertyNames, params CheckType[] checkTypes)
        {
            _propertyInfos = propertyNames.Select(name => entityType.GetProperty(name)).ToArray();
            _checkTypes = checkTypes;
            string typeName = typeof(T).Name;
            string pluralForm = Pluralizer.GetPluralForm(typeName);
            var databaseSettings = new DatabaseSettings()
            {
                ConnectionStrings = "mongodb://localhost:27017",
                DatabaseName = "MultiShopCatalogDb",
                CategoryCollectionName = pluralForm,
            };

            var client = new MongoClient(databaseSettings.ConnectionStrings);
            var database = client.GetDatabase(databaseSettings.DatabaseName);

            _collection = database.GetCollection<T>(pluralForm);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var propertyInfo in _propertyInfos)
            {
                var propertyValue = context.ActionArguments.Values.FirstOrDefault();
                if (propertyValue == null)
                {
                    context.Result = new BadRequestObjectResult($"'{propertyInfo.Name}' alani bulunamadı");
                    return;
                }

                // _propertyInfo üzerinden değeri alıyoruz
                var value = propertyInfo.GetValue(propertyValue)?.ToString();

                foreach (var checkType in _checkTypes)
                {
                    var filter = Builders<T>.Filter.Eq(propertyInfo.Name, value);
                    var exists = await _collection.Find(filter).AnyAsync();

                    if (checkType == CheckType.Existing)
                    {
                        if (exists)
                        {
                            context.Result = new BadRequestObjectResult($"'{propertyInfo.Name}' - '{value}' - değeri daha önceden sisteme kayıt edilmiştir");
                            return;
                        }
                    }
                }
            }
            await next();
        }
    }
}
