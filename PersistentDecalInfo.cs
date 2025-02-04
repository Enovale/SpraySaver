using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SpraySaver
{
    public struct PersistentDecalInfo
    {
        public Color Color;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector2 Scale;
        public DecalLayerEnum LayerMask;
        public string ParentPath;

        public override string ToString()
        {
            return $"{Color}, {LayerMask}, {Position}, {Rotation}, {Scale}, {ParentPath}";
        }
    }
}