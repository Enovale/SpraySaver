using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SpraySaver.Util
{
    public static class HelperExtensions
    {
        public static string GetFullPath(this Transform tr)
        {
            var parents = tr.GetComponentsInParent<Transform>();

            var str = new StringBuilder('/' + parents[^1].name);
            for (var i = parents.Length - 2; i >= 0; i--)
                str.Append($"/{parents[i].name}");

            return str.ToString();
        }

        public static void Replace<T>(this List<T> list, List<T> newList)
        {
            list.Clear();
            list.AddRange(newList);
        }
        
        public static bool IsChildOf(this Transform instance, params Transform?[] parents)
        {
            return instance != null && parents.Length > 0 && parents.Any(t => t != null && instance.IsChildOf(t));
        }
        
        public static bool IsChildOf(this GameObject? instance, params Transform?[] parents)
        {
            return instance != null && IsChildOf(instance.transform, parents);
        }
        
        public static bool IsChildOf(this GameObject? instance, params string[] parents)
        {
            return instance != null && IsChildOf(instance.transform, parents);
        }
        
        public static bool IsChildOf(this Transform? instance, params string[] parents)
        {
            return instance != null && parents.Length > 0 && parents.Any(p => p != null && instance.GetFullPath().StartsWith(p));
        }
        
        public static bool IsParentOf(this GameObject? instance, string child)
        {
            return instance != null && IsParentOf(instance.transform, child);
        }
        
        public static bool IsParentOf(this Transform? instance, string child)
        {
            return instance != null && child.StartsWith(instance.GetFullPath());
        }
    }
}