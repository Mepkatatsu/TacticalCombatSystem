using System.Collections;

namespace Script.CommonLib
{
    public static class Extensions
    {
        public static bool IsEmpty(this ICollection collection)
        {
            return collection == null || collection.Count == 0;
        }
        
        public static bool IsNotEmpty(this ICollection collection)
        {
            return collection != null && collection.Count > 0;
        }
    }
}
