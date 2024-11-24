namespace Inochi2dSharp.Core.Automations;

public static class AutomationHelper
{
    private static Dictionary<string, Func<Puppet, Automation>> typeFactories = [];

    public static void Init()
    {
        RegisterAutomationType<PhysicsAutomation>();
        RegisterAutomationType<SineAutomation>();
    }

    public static void RegisterAutomationType<T>() where T : Automation
    {
        var typeId = GetTypeId<T>();
        typeFactories.Add(typeId, (Puppet parent) => (Activator.CreateInstance(typeof(T), parent) as Automation)!);
    }

    public static Automation InstantiateAutomation(string id, Puppet parent)
    {
        if (typeFactories.TryGetValue(id, out var factory))
        {
            return factory(parent);
        }
        throw new KeyNotFoundException($"No automation type registered with id {id}");
    }

    public static bool HasAutomationType(string id)
    {
        return typeFactories.ContainsKey(id);
    }

    private static string GetTypeId<T>()
    {
        // Simulate the behavior of D's getUDAs to retrieve a unique type identifier
        // In C#, this might be done using custom attributes or a simple type name
        var attributes = typeof(T).GetCustomAttributes(typeof(TypeIdAttribute), false);
        if (attributes.Length > 0)
        {
            return ((TypeIdAttribute)attributes[0]).Id;
        }
        return typeof(T).Name; // Fallback to type name if no attribute is found
    }
}
