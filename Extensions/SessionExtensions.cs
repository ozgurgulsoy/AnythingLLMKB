using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace TestKB.Extensions
{
    /// <summary>
    /// Session işlemleri için extension metotları
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Integer değeri session'a kaydeder
        /// </summary>
        public static void SetInt32(this ISession session, string key, int value)
        {
            session.Set(key, BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Session'dan Integer değeri getirir
        /// </summary>
        public static int? GetInt32(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null || data.Length < 4)
            {
                return null;
            }
            return BitConverter.ToInt32(data, 0);
        }
        
        /// <summary>
        /// Enum değerini session'a kaydeder
        /// </summary>
        public static void SetEnum<T>(this ISession session, string key, T value) where T : struct
        {
            session.SetInt32(key, Convert.ToInt32(value));
        }
        
        /// <summary>
        /// Session'dan Enum değerini getirir
        /// </summary>
        public static T? GetEnum<T>(this ISession session, string key) where T : struct
        {
            var value = session.GetInt32(key);
            if (value.HasValue)
            {
                return (T)Enum.ToObject(typeof(T), value.Value);
            }
            return null;
        }
        
        /// <summary>
        /// String değeri session'a kaydeder
        /// </summary>
        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty));
        }
        
        /// <summary>
        /// Session'dan String değerini getirir
        /// </summary>
        public static string GetString(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return System.Text.Encoding.UTF8.GetString(data);
        }
        
        /// <summary>
        /// Herhangi bir nesneyi JSON olarak session'a kaydeder
        /// </summary>
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            session.SetString(key, json);
        }
        
        /// <summary>
        /// Session'dan JSON formatındaki nesneyi getirir
        /// </summary>
        public static T GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(json);
        }
        
        /// <summary>
        /// Session'dan bir anahtarı siler
        /// </summary>
        public static void Remove(this ISession session, string key)
        {
            session.Remove(key);
        }
        
        /// <summary>
        /// Belirli bir anahtarın session'da olup olmadığını kontrol eder
        /// </summary>
        public static bool ContainsKey(this ISession session, string key)
        {
            return session.Keys.Contains(key);
        }
    }
}