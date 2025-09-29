using System.Runtime.InteropServices;

namespace Inochi2dSharp.Core.Render;

/// <summary>
/// A draw list containing the rendering state and commands to submit to the GPU.
/// </summary>
public class DrawList : IDisposable
{
    // Working set
    private DrawListAlloc _call;
    private DrawCmd _ccmd;

    // Draw Commands
    private readonly List<DrawCmd> _cmds = [];
    private int _cmdp;

    // Vertex Data
    private VtxData[] _vtxs = [];
    private int _vtxp;

    // Index Data
    private uint[] _idxs = [];
    private int _idxp;

    // Buffer allocations
    private readonly List<DrawListAlloc> _allocs = [];
    private int _allp;

    // Stacks
    private readonly Stack<Texture[]> _targetsStack = [];

    /// <summary>
    /// Whether to use base vertex specification.
    /// </summary>
    public bool UseBaseVertex = true;

    /// <summary>
    /// Command Buffer
    /// </summary>
    public List<DrawCmd> Commands => _cmds;

    /// <summary>
    /// Vertex data
    /// </summary>
    public VtxData[] Vertices => _vtxs;

    /// <summary>
    /// Index data
    /// </summary>
    public uint[] Indices => _idxs;

    /// <summary>
    /// Allocated meshes
    /// </summary>
    /// <returns></returns>
    public List<DrawListAlloc> Allocations => _allocs;

    public DrawListAlloc? Allocate(VtxData[] vtx, uint[] idx)
    {
        // Invalid vertex buffer check.
        if (vtx.Length < 3)
            return null;

        // Invalid index buffer check.
        if (idx.Length != 0 && (idx.Length % 3) != 0)
            return null;

        // Meshes supply their own index data, as such
        // we offset it here to fit within our buffer.
        if (!UseBaseVertex)
        {
            for (int i = 0; i < idx.Length; i++)
            {
                idx[i] += (uint)_idxp;
            }
        }

        var list = new List<VtxData>(_vtxs);
        list.AddRange(vtx);
        _vtxs = [.. list];
        var list1 = new List<uint>(_idxs);
        list1.AddRange(idx);
        _idxs = [.. list1];
        _vtxp += vtx.Length;
        _idxp += idx.Length;

        _call.AllocId = _allp;
        _call.IdxCount = idx.Length;
        _call.VtxCount = vtx.Length;

        // Set up allocation.
        if (_allp >= _allocs.Count)
            _allocs.Add(_call);
        else
            _allocs[_allp] = _call;

        // prepare next alloc
        _call = new DrawListAlloc
        {
            IdxOffset = _idxp,
            VtxOffset = _vtxp
        };
        return _allocs[_allp++];
    }

    /// <summary>
    /// Pushes render targets to the draw list's stack.
    /// </summary>
    public void BeginComposite()
    {
        _ccmd.State = DrawState.CompositeBegin;
        Next();
    }

    /// <summary>
    /// Pops the top render target from the list's stack.
    /// </summary>
    public void EndComposite()
    {
        _ccmd.State = DrawState.CompositeEnd;
        Next();
    }

    /// <summary>
    /// Sets sources for the current draw call.
    /// </summary>
    /// <param name="sources"></param>
    public void SetSources(Texture[] sources)
    {
        if (sources.Length != DrawCmd.IN_MAX_ATTACHMENTS)
        {
            throw new Exception("sources length is not " + DrawCmd.IN_MAX_ATTACHMENTS);
        }
        _ccmd.Sources = sources;
    }

    /// <summary>
    /// Sets the blending mode for the current draw call.
    /// </summary>
    /// <param name="value"></param>
    public void SetBlending(BlendMode value)
    {
        _ccmd.BlendMode = value;
    }

    /// <summary>
    /// Sets the blending mode for the current draw call.
    /// </summary>
    /// <param name="nid"></param>
    /// <param name="value"></param>
    /// <param name="size"></param>
    /// <exception cref="Exception"></exception>
    public unsafe void SetVariables(uint nid, void* value, int size)
    {
        _ccmd.TypeId = nid;
        Buffer.MemoryCopy(value, (void*)_ccmd.Variables, 64, size);
    }

    /// <summary>
    /// Sets the masking mode for the current draw call.
    /// </summary>
    /// <param name="value"></param>
    public void SetMasking(MaskingMode value)
    {
        _ccmd.MaskMode = value;
    }

    /// <summary>
    /// Sets the mesh data for the current draw command.
    /// </summary>
    /// <param name="alloc">The vertex allocation cookie.</param>
    public void SetMesh(DrawListAlloc? alloc)
    {
        if (alloc == null) return;

        _ccmd.AllocId = alloc.AllocId;
        _ccmd.IdxOffset = alloc.IdxOffset;
        _ccmd.VtxOffset = alloc.VtxOffset;
        _ccmd.ElemCount = alloc.IdxCount;
    }

    /// <summary>
    /// Sets the active state for the current command.
    /// </summary>
    /// <param name="state"></param>
    public void SetDrawState(DrawState state)
    {
        _ccmd.State = state;
    }

    /// <summary>
    /// Pushes the next draw command
    /// </summary>
    public void Next()
    {
        if (_cmdp >= _cmds.Count)
            _cmds.Add(_ccmd);
        else
            _cmds[_cmdp] = _ccmd;

        _cmdp++;
        _ccmd = new DrawCmd();
    }

    public void Clear()
    {
        _vtxp = 0;
        _idxp = 0;
        _cmdp = 0;
        _allp = 0;
        _ccmd = new DrawCmd();
        _call = new DrawListAlloc();
        _targetsStack.Clear();
    }

    public void Dispose()
    {
        foreach (var item in _cmds)
        {
            item.Dispose();
        }
        _cmds.Clear();
        _allocs.Clear();
        _targetsStack.Clear();
    }
}
