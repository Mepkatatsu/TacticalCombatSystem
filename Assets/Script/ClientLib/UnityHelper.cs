using UnityEngine;

public static class UnityHelper
{
    public static T GetParent<T>(this GameObject gameObject) where T : Component
    {
        var parent = gameObject.transform.parent;

        while (parent)
        {
            if (parent.TryGetComponent<T>(out var component))
                return component;
            
            parent = parent.transform.parent;
        }

        return null;
    }
}
