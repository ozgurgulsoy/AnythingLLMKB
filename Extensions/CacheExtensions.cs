
using System.Reflection;
using System.Text;

namespace TestKB.Extensions;

public static class CacheExtensions
{
    /// <summary>
    /// Nesnenin bir veya daha fazla özelliğine dayalı önbellek anahtarı oluşturur.
    /// </summary>
    /// <param name="instance">Özelliği içeren nesne</param>
    /// <param name="prefix">Önbellek anahtarı öneki</param>
    /// <param name="propertyNames">Anahtara dahil edilecek özellik isimleri</param>
    /// <returns>Önbellek anahtarı</returns>
    public static string CreateCacheKey(this object instance, string prefix, params string[] propertyNames)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        var keyBuilder = new StringBuilder(prefix);
        var instanceType = instance.GetType();

        foreach (var propName in propertyNames)
        {
            var property = instanceType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                continue;

            var value = property.GetValue(instance)?.ToString() ?? "null";
            keyBuilder.Append($":{propName}={value}");
        }

        return keyBuilder.ToString();
    }

    public static string CreateCacheKey<T>(string prefix = "")
    {
        return $"{prefix}{typeof(T).Name}";
    }

    /// <summary>
    /// Belirli bir tipe ve ID'ye göre önbellek anahtarı oluşturur.
    /// </summary>
    /// <typeparam name="T">Anahtar için kullanılacak tip</typeparam>
    /// <param name="id">Nesne ID'si</param>
    /// <returns>Önbellek anahtarı</returns>
    public static string CreateCacheKey<T>(object id)
    {
        return $"{typeof(T).Name}:{id}";
    }

    /// <summary>
    /// İçerik öğeleri için önbellek anahtarı oluşturur.
    /// </summary>
    /// <param name="category">İsteğe bağlı kategori filtresi</param>
    /// <returns>Önbellek anahtarı</returns>
    public static string CreateContentItemsCacheKey(string? category = null)
    {
        return string.IsNullOrWhiteSpace(category)
            ? "ContentItems:All"
            : $"ContentItems:Category={category}";
    }
}