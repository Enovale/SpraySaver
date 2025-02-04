using System.Text;
using UnityEngine;

namespace SpraySaver.Util
{
    public static class HelperExtensions
    {
        public static string GetFullPath(this Transform tr)
        {
            var parents = tr.GetComponentsInParent<Transform>();

            var str = new StringBuilder(parents[^1].name);
            for (var i = parents.Length - 2; i >= 0; i--)
                str.Append($"/{parents[i].name}");

            return str.ToString();
        }
    }
}