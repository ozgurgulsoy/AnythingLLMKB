using Microsoft.AspNetCore.Http;
using TestKB.Models;

namespace TestKB.Extensions
{
    /// <summary>
    /// Department ile ilgili session işlemlerini kolaylaştıran extension metotları
    /// </summary>
    public static class DepartmentExtensions
    {
        private const string DepartmentSessionKey = "SelectedDepartment";

        /// <summary>
        /// Seçili departmanı session'a kaydeder
        /// </summary>
        public static void SetSelectedDepartment(this ISession session, Department department)
        {
            session.SetInt32(DepartmentSessionKey, (int)department);
        }

        /// <summary>
        /// Session'dan seçili departmanı getirir. Eğer departman seçili değilse
        /// varsayılan olarak Yazılım departmanını döndürür.
        /// </summary>
        public static Department GetSelectedDepartment(this ISession session)
        {
            var value = session.GetInt32(DepartmentSessionKey);
            return value.HasValue ? (Department)value.Value : Department.Yazılım; // Varsayılan olarak Yazılım departmanı
        }

        /// <summary>
        /// Department enum değerini string olarak getirir
        /// </summary>
        public static string GetDepartmentName(this Department department)
        {
            return department.ToString();
        }

        /// <summary>
        /// String değerini Department enum'a dönüştürür
        /// </summary>
        public static Department ParseDepartment(string departmentName)
        {
            if (string.IsNullOrEmpty(departmentName))
                return Department.Yazılım;

            if (Enum.TryParse<Department>(departmentName, true, out var result))
                return result;

            return Department.Yazılım;
        }
    }
}