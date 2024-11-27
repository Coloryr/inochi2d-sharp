using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Automations;

[TypeId("physics")]
internal class PhysicsAutomation : Automation
{
    /// <summary>
    /// A node in the internal verlet simulation
    /// </summary>
    public List<VerletNode> Nodes = [];

    /// <summary>
    /// Amount of damping to apply to movement
    /// </summary>
    public float Damping = 0.05f;

    /// <summary>
    /// How bouncy movement should be
    /// 1 = default bounciness
    /// </summary>
    public float Bounciness = 1f;

    /// <summary>
    /// Gravity to apply to each link
    /// </summary>
    public float Gravity = 20f;

    private readonly I2dTime _time;

    public PhysicsAutomation(Puppet parent, I2dTime time) : base(parent)
    {
        _time = time;
        TypeId = "physics";
    }

    /// <summary>
    /// Adds a binding
    /// </summary>
    /// <param name="binding"></param>
    public override void Bind(AutomationBinding binding)
    {
        base.Bind(binding);

        Nodes.Add(new VerletNode(new Vector2(0, 1)));
    }

    protected void Simulate(int i, AutomationBinding binding)
    {
        var node = Nodes[i];

        var tmp = node.Position;
        node.Position = node.Position - node.OldPosition + new Vector2(0, Gravity) * (_time.DeltaTime() * _time.DeltaTime()) * Bounciness;
        node.OldPosition = tmp;
    }

    protected void Constrain()
    {
        for (int i = 0; i < Nodes.Count - 1; i++)
        {
            var node1 = Nodes[i];
            var node2 = Nodes[i + 1];

            // idx 0 = first node in param, this always is the reference node.
            // We base our "hinge" of the value of this reference value
            if (i == 0)
            {
                node1.Position = new Vector2(Bindings[i].GetAxisValue(), 0);
            }

            // Then we calculate the distance of the difference between
            // node 1 and 2, 
            float diffX = node1.Position.X - node2.Position.X;
            float diffY = node1.Position.Y - node2.Position.Y;
            float dist = Vector2.Distance(node1.Position, node2.Position);
            float diff = 0;

            // We need the distance to be larger than 0 so that
            // we don't get division by zero problems.
            if (dist > 0)
            {
                // Node2 decides how far away it wants to be from Node1
                diff = (node2.Distance - dist) / dist;
            }

            // Apply out fancy new link
            var trans = new Vector2(diffX, diffY) * (0.5f * diff);
            node1.Position += trans;
            node2.Position -= trans;

            // Clamp so that we don't start flopping above the hinge above us
            node2.Position.Y = float.Clamp(node2.Position.Y, node1.Position.Y, float.MaxValue);
        }
    }

    protected override void OnUpdate()
    {
        if (Bindings.Count > 1)
        {
            // simulate each link in our chain
            for (int i = 0; i < Bindings.Count; i++)
            {
                var binding = Bindings[i];
                Simulate(i, binding);
            }

            // Constrain values
            for (int i = 0; i < 4 + (Bindings.Count * 2); i++)
            {
                Constrain();
            }

            // Clamp and apply everything to be within range
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                node.Position.X = float.Clamp(node.Position.X, Bindings[i].Range.X, Bindings[i].Range.Y);
                Bindings[i].AddAxisOffset(node.Position.X);
            }
        }
    }

    protected override void SerializeSelf(JsonObject serializer)
    {
        var list = new JsonArray();
        foreach (var item in Nodes)
        {
            var obj = new JsonObject();
            item.Serialize(obj);
            list.Add(obj);
        }
        serializer.Add("nodes", list);
        serializer.Add("damping", Damping);
        serializer.Add("bounciness", Bounciness);
        serializer.Add("gravity", Gravity);
    }

    protected override void DeserializeSelf(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "nodes" && item.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item1 in item.Value.EnumerateArray())
                {
                    var node = new VerletNode();
                    node.Deserialize(item1);
                    Nodes.Add(node);
                }
            }
            else if (item.Name == "damping" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Damping = item.Value.GetSingle();
            }

            else if (item.Name == "bounciness" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Bounciness = item.Value.GetSingle();
            }
            else if (item.Name == "gravity" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Gravity = item.Value.GetSingle();
            }
        }
    }
}
