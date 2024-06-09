using UnityEngine;
using System.Reflection;

public static class GameObjectExtensions
{
    public static void ActivateAllComponents(this GameObject gameObject)
    {
        Component[] components = gameObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            PropertyInfo enabledProperty = component.GetType().GetProperty("enabled");
            if (enabledProperty != null && enabledProperty.PropertyType == typeof(bool))
            {
                enabledProperty.SetValue(component, true);
            }
        }
    }
}
