namespace Inochi2dSharp.Core.Render;

/// <summary>
/// A cache of textures in use by a model.
/// </summary>
public class TextureCache
{
    private readonly List<Texture> _textures = [];

    public int Size => _textures.Count;
    public List<Texture> Cache => _textures;

    /// <summary>
    /// Adds a texture to the cache, adding a retain count to the texture. Texture caches only allow a single instance of a texture to be stored within.
    /// </summary>
    /// <param name="texture">The texture to add to the cache.</param>
    /// <returns>The texture slot position of the added texture.</returns>
    public int Add(Texture texture)
    {
        var idx = _textures.IndexOf(texture);
        if (idx == -1)
        {
            texture.Retain();
            _textures.Add(texture);

            return _textures.Count - 1;
        }
        return idx;
    }

    /// <summary>
    /// Prunes all textures from the cache, only leaving behind textures referenced from outside of the cache.
    /// <br/>
    /// Any texture that is unused will be freed.
    /// </summary>
    public void Prune()
    {
        var alive = 0;
        foreach (var item in _textures.ToArray())
        {
            if (item.Released())
            {
                // Avoid copy semantics, moving the alive texture
                // back to the lowest slot now available.
                // Then restore its refcount held by the cache.
                alive++;
                item.Retain();
            }
            else
            {
                _textures.Remove(item);
            }
        }
    }

    /// <summary>
    /// Tries to get a texture from the cache.
    /// </summary>
    /// <param name="slotId">The texture slot ID to try to fetch.</param>
    /// <returns>The given texture if found, <see langword="null"/> otherwise.</returns>
    public Texture? Get(int slotId)
    {
        return slotId >= Size ? null : _textures[slotId];
    }

    /// <summary>
    /// Finds the slot of a given texture within the cache.
    /// </summary>
    /// <param name="texture">The texture to look for.</param>
    /// <returns> A non-negative number on success, -1 if the texture was not found.</returns>
    public int Find(Texture texture)
    {
        return _textures.IndexOf(texture);
    }
}
