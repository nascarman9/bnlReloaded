using System.Numerics;
using Octree;

namespace BNLReloadedServer.Octree_Extensions;

#nullable disable
public static class BoundingBoxExtensions
{
  public static bool Intersects(this BoundingBox box, IBoundingShape shape) => shape.Intersects(box);
}

public class BoundsOctreeEx<T>
{
  private Node _rootNode;
  /// <summary>
  /// Should be a value between 1 and 2. A multiplier for the base size of a node.
  /// </summary>
  /// <remarks>
  /// 1.0 is a "normal" octree, while values &gt; 1 have overlap
  /// </remarks>
  private readonly float _looseness;
  /// <summary>Size that the octree was on creation</summary>
  private readonly float _initialSize;
  /// <summary>
  /// Minimum side length that a node can be - essentially an alternative to having a max depth
  /// </summary>
  private readonly float _minSize;

  /// <summary>The total amount of objects currently in the tree</summary>
  public int Count { get; private set; }

  /// <summary>
  /// Gets the bounding box that represents the whole octree
  /// </summary>
  /// <value>The bounding box of the root node.</value>
  public BoundingBox MaxBounds => _rootNode.Bounds;

  /// <summary>
  /// Gets All the bounding box that represents the whole octree
  /// </summary>
  /// <returns></returns>
  public BoundingBox[] GetChildBounds()
  {
    var bounds = new List<BoundingBox>();
    _rootNode.GetChildBounds(bounds);
    return bounds.ToArray();
  }

  /// <summary>Constructor for the bounds octree.</summary>
  /// <param name="initialWorldSize">Size of the sides of the initial node, in metres. The octree will never shrink smaller than this.</param>
  /// <param name="initialWorldPos">Position of the center of the initial node.</param>
  /// <param name="minNodeSize">Nodes will stop splitting if the new nodes would be smaller than this (metres).</param>
  /// <param name="loosenessVal">Clamped between 1 and 2. Values &gt; 1 let nodes overlap.</param>
  /// <exception cref="T:System.ArgumentException">Minimum node size must be at least as big as the initial world size.</exception>
  public BoundsOctreeEx(
    float initialWorldSize,
    Vector3 initialWorldPos,
    float minNodeSize,
    float loosenessVal)
  {
    if (minNodeSize > (double) initialWorldSize)
      throw new ArgumentException("Minimum node size must be at least as big as the initial world size.", nameof (minNodeSize));
    Count = 0;
    _initialSize = initialWorldSize;
    _minSize = minNodeSize;
    _looseness = MathExtensions.Clamp(loosenessVal, 1f, 2f);
    _rootNode = new Node(_initialSize, _minSize, _looseness, initialWorldPos);
  }

  /// <summary>Add an object.</summary>
  /// <param name="obj">Object to add.</param>
  /// <param name="objBounds">3D bounding box around the object.</param>
  /// <exception cref="T:System.InvalidOperationException">Add operation required growing the octree too much.</exception>
  public void Add(T obj, BoundingBox objBounds)
  {
    var num = 0;
    while (!_rootNode.Add(obj, objBounds))
    {
      Grow(objBounds.Center - _rootNode.Center);
      if (++num > 20)
        throw new InvalidOperationException("Aborted Add operation as it seemed to be going on forever " + $"({num - 1} attempts at growing the octree).");
    }
    ++Count;
  }

  /// <summary>
  /// Remove an object. Makes the assumption that the object only exists once in the tree.
  /// </summary>
  /// <param name="obj">Object to remove.</param>
  /// <returns>True if the object was removed successfully.</returns>
  public bool Remove(T obj)
  {
    var num = _rootNode.Remove(obj) ? 1 : 0;
    if (num == 0)
      return false;
    --Count;
    Shrink();
    return true;
  }

  /// <summary>
  /// Removes the specified object at the given position. Makes the assumption that the object only exists once in the tree.
  /// </summary>
  /// <param name="obj">Object to remove.</param>
  /// <param name="objBounds">3D bounding box around the object.</param>
  /// <returns>True if the object was removed successfully.</returns>
  public bool Remove(T obj, BoundingBox objBounds)
  {
    var num = _rootNode.Remove(obj, objBounds) ? 1 : 0;
    if (num == 0)
      return false;
    --Count;
    Shrink();
    return true;
  }

  /// <summary>
  /// Check if the specified bounds intersect with anything in the tree. See also: GetColliding.
  /// </summary>
  /// <param name="checkBounds">bounds to check.</param>
  /// <returns>True if there was a collision.</returns>
  public bool IsColliding(BoundingBox checkBounds) => _rootNode.IsColliding(ref checkBounds);
  
  public bool IsColliding(IBoundingShape checkBounds) => _rootNode.IsColliding(ref checkBounds);

  /// <summary>
  /// Check if the specified ray intersects with anything in the tree. See also: GetColliding.
  /// </summary>
  /// <param name="checkRay">ray to check.</param>
  /// <param name="maxDistance">distance to check.</param>
  /// <returns>True if there was a collision.</returns>
  public bool IsColliding(Ray checkRay, float maxDistance)
  {
    return _rootNode.IsColliding(ref checkRay, maxDistance);
  }

  /// <summary>
  /// Returns an array of objects that intersect with the specified bounds, if any.
  /// Otherwise returns an empty array.
  /// </summary>
  /// <seealso cref="M:Octree.BoundsOctree`1.IsColliding(Octree.BoundingBox)" />
  /// <param name="checkBounds">bounds to check.</param>
  /// <returns>Objects that intersect with the specified bounds.</returns>
  public T[] GetColliding(BoundingBox checkBounds)
  {
    var result = new List<T>();
    _rootNode.GetColliding(ref checkBounds, result);
    return result.ToArray();
  }
  
  public T[] GetColliding(IBoundingShape checkBounds)
  {
    var result = new List<T>();
    _rootNode.GetColliding(ref checkBounds, result);
    return result.ToArray();
  }

  /// <summary>
  /// Returns an array of objects that intersect with the specified ray, if any.
  /// Otherwise returns an empty array.
  /// </summary>
  /// <seealso cref="M:Octree.BoundsOctree`1.IsColliding(Octree.BoundingBox)" />
  /// <param name="checkRay">ray to check.</param>
  /// <param name="maxDistance">distance to check.</param>
  /// <returns>Objects that intersect with the specified ray.</returns>
  public T[] GetColliding(Ray checkRay, float maxDistance = float.PositiveInfinity)
  {
    var result = new List<T>();
    _rootNode.GetColliding(ref checkRay, result, maxDistance);
    return result.ToArray();
  }

  /// <summary>
  /// Returns an array of objects that intersect with the specified bounds, if any.
  /// Otherwise returns an empty array.
  /// </summary>
  /// <seealso cref="M:Octree.BoundsOctree`1.IsColliding(Octree.BoundingBox)" />
  /// <param name="collidingWith">list to store intersections.</param>
  /// <param name="checkBounds">bounds to check.</param>
  /// <returns><c>true</c> if items are found, <c>false</c> otherwise.</returns>
  public bool GetCollidingNonAlloc(List<T> collidingWith, BoundingBox checkBounds)
  {
    collidingWith.Clear();
    _rootNode.GetColliding(ref checkBounds, collidingWith);
    return collidingWith.Count > 0;
  }
  
  public bool GetCollidingNonAlloc(List<T> collidingWith, IBoundingShape checkBounds)
  {
    collidingWith.Clear();
    _rootNode.GetColliding(ref checkBounds, collidingWith);
    return collidingWith.Count > 0;
  }

  /// <summary>
  /// Returns an array of objects that intersect with the specified ray, if any.
  /// Otherwise returns an empty array.
  /// </summary>
  /// <seealso cref="M:Octree.BoundsOctree`1.IsColliding(Octree.BoundingBox)" />
  /// <param name="collidingWith">list to store intersections.</param>
  /// <param name="checkRay">ray to check.</param>
  /// <param name="maxDistance">distance to check.</param>
  /// <returns><c>true</c> if items are found, <c>false</c> otherwise.</returns>
  public bool GetCollidingNonAlloc(List<T> collidingWith, Ray checkRay, float maxDistance = float.PositiveInfinity)
  {
    collidingWith.Clear();
    _rootNode.GetColliding(ref checkRay, collidingWith, maxDistance);
    return collidingWith.Count > 0;
  }

  /// <summary>Grow the octree to fit in all objects.</summary>
  /// <param name="direction">Direction to grow.</param>
  private void Grow(Vector3 direction)
  {
    var num1 = direction.X >= 0.0 ? 1 : -1;
    var num2 = direction.Y >= 0.0 ? 1 : -1;
    var num3 = direction.Z >= 0.0 ? 1 : -1;
    var rootNode = _rootNode;
    var num4 = _rootNode.BaseLength / 2f;
    var baseLengthVal = _rootNode.BaseLength * 2f;
    var centerVal = _rootNode.Center + new Vector3(num1 * num4, num2 * num4, num3 * num4);
    _rootNode = new Node(baseLengthVal, _minSize, _looseness, centerVal);
    if (!rootNode.HasAnyObjects())
      return;
    var num5 = _rootNode.BestFitChild(rootNode.Center);
    var childOctrees = new Node[8];
    for (var index = 0; index < 8; ++index)
    {
      if (index == num5)
      {
        childOctrees[index] = rootNode;
      }
      else
      {
        var num6 = index % 2 == 0 ? -1 : 1;
        var num7 = index > 3 ? -1 : 1;
        var num8 = index is < 2 or > 3 and < 6 ? -1 : 1;
        childOctrees[index] = new Node(rootNode.BaseLength, _minSize, _looseness, centerVal + new Vector3(num6 * num4, num7 * num4, num8 * num4));
      }
    }
    _rootNode.SetChildren(childOctrees);
  }

  /// <summary>
  /// Shrink the octree if possible, else leave it the same.
  /// </summary>
  private void Shrink() => _rootNode = _rootNode.ShrinkIfPossible(_initialSize);

  /// <summary>A node in a BoundsOctree</summary>
  private class Node
  {
    /// <summary>Looseness value for this node</summary>
    private float _looseness;
    /// <summary>Minimum size for a node in this octree</summary>
    private float _minSize;
    /// <summary>
    /// Actual length of sides, taking the looseness value into account
    /// </summary>
    private float _adjLength;
    /// <summary>Bounding box that represents this node</summary>
    private BoundingBox _bounds;
    /// <summary>Objects in this node</summary>
    private readonly List<OctreeObject> _objects = [];
    /// <summary>Child nodes, if any</summary>
    private Node[] _children;
    /// <summary>
    /// Bounds of potential children to this node. These are actual size (with looseness taken into account), not base size
    /// </summary>
    private BoundingBox[] _childBounds;
    /// <summary>
    /// If there are already NumObjectsAllowed in a node, we split it into children
    /// </summary>
    /// <remarks>
    /// A generally good number seems to be something around 8-15
    /// </remarks>
    private const int NumObjectsAllowed = 8;

    /// <summary>Centre of this node</summary>
    public Vector3 Center { get; private set; }

    /// <summary>Length of this node if it has a looseness of 1.0</summary>
    public float BaseLength { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this node has children
    /// </summary>
    private bool HasChildren => _children != null;

    /// <summary>Gets the bounding box that represents this node</summary>
    public BoundingBox Bounds => _bounds;

    /// <summary>Gets All the bounding box that represents this node</summary>
    /// <param name="bounds"></param>
    public void GetChildBounds(List<BoundingBox> bounds)
    {
      if (HasChildren)
      {
        foreach (var child in _children)
          child.GetChildBounds(bounds);
      }
      else
        bounds.Add(Bounds);
    }

    /// <summary>Constructor.</summary>
    /// <param name="baseLengthVal">Length of this node, not taking looseness into account.</param>
    /// <param name="minSizeVal">Minimum size of nodes in this octree.</param>
    /// <param name="loosenessVal">Multiplier for baseLengthVal to get the actual size.</param>
    /// <param name="centerVal">Centre position of this node.</param>
    public Node(float baseLengthVal, float minSizeVal, float loosenessVal, Vector3 centerVal)
    {
      SetValues(baseLengthVal, minSizeVal, loosenessVal, centerVal);
    }

    /// <summary>Add an object.</summary>
    /// <param name="obj">Object to add.</param>
    /// <param name="objBounds">3D bounding box around the object.</param>
    /// <returns>True if the object fits entirely within this node.</returns>
    public bool Add(T obj, BoundingBox objBounds)
    {
      if (!Encapsulates(_bounds, objBounds))
        return false;
      SubAdd(obj, objBounds);
      return true;
    }

    /// <summary>
    /// Remove an object. Makes the assumption that the object only exists once in the tree.
    /// </summary>
    /// <param name="obj">Object to remove.</param>
    /// <returns>True if the object was removed successfully.</returns>
    public bool Remove(T obj)
    {
      var flag = false;
      for (var index = 0; index < _objects.Count; ++index)
      {
        if (!_objects[index].Obj.Equals(obj)) continue;
        flag = _objects.Remove(_objects[index]);
        break;
      }
      if (!flag && _children != null)
      {
        for (var index = 0; index < 8; ++index)
        {
          flag = _children[index].Remove(obj);
          if (flag)
            break;
        }
      }
      if (flag && _children != null && ShouldMerge())
        Merge();
      return flag;
    }

    /// <summary>
    /// Removes the specified object at the given position. Makes the assumption that the object only exists once in the tree.
    /// </summary>
    /// <param name="obj">Object to remove.</param>
    /// <param name="objBounds">3D bounding box around the object.</param>
    /// <returns>True if the object was removed successfully.</returns>
    public bool Remove(T obj, BoundingBox objBounds)
    {
      return Encapsulates(_bounds, objBounds) && SubRemove(obj, objBounds);
    }

    /// <summary>
    /// Check if the specified bounds intersect with anything in the tree. See also: GetColliding.
    /// </summary>
    /// <param name="checkBounds">Bounds to check.</param>
    /// <returns>True if there was a collision.</returns>
    public bool IsColliding(ref BoundingBox checkBounds)
    {
      if (!_bounds.Intersects(checkBounds))
        return false;
      foreach (var t in _objects)
      {
        if (t.Bounds.Intersects(checkBounds))
          return true;
      }

      if (_children == null) return false;
      for (var index = 0; index < 8; ++index)
      {
        if (_children[index].IsColliding(ref checkBounds))
          return true;
      }
      return false;
    }
    
    public bool IsColliding(ref IBoundingShape checkBounds)
    {
      if (!_bounds.Intersects(checkBounds))
        return false;
      foreach (var t in _objects)
      {
        if (t.Bounds.Intersects(checkBounds))
          return true;
      }

      if (_children == null) return false;
      for (var index = 0; index < 8; ++index)
      {
        if (_children[index].IsColliding(ref checkBounds))
          return true;
      }
      return false;
    }

    /// <summary>
    /// Check if the specified ray intersects with anything in the tree. See also: GetColliding.
    /// </summary>
    /// <param name="checkRay">Ray to check.</param>
    /// <param name="maxDistance">Distance to check.</param>
    /// <returns>True if there was a collision.</returns>
    public bool IsColliding(ref Ray checkRay, float maxDistance = float.PositiveInfinity)
    {
      if (!_bounds.IntersectRay(checkRay, out var distance) || distance > (double) maxDistance)
        return false;
      foreach (var t in _objects)
      {
        if (t.Bounds.IntersectRay(checkRay, out distance) && distance <= (double) maxDistance)
          return true;
      }

      if (_children == null) return false;
      for (var index = 0; index < 8; ++index)
      {
        if (_children[index].IsColliding(ref checkRay, maxDistance))
          return true;
      }
      return false;
    }

    /// <summary>
    /// Returns an array of objects that intersect with the specified bounds, if any. Otherwise returns an empty array. See also: IsColliding.
    /// </summary>
    /// <param name="checkBounds">Bounds to check. Passing by ref as it improves performance with structs.</param>
    /// <param name="result">List result.</param>
    /// <returns>Objects that intersect with the specified bounds.</returns>
    public void GetColliding(ref BoundingBox checkBounds, List<T> result)
    {
      if (!_bounds.Intersects(checkBounds))
        return;
      foreach (var t in _objects)
      {
        if (t.Bounds.Intersects(checkBounds))
          result.Add(t.Obj);
      }
      if (_children == null)
        return;
      for (var index = 0; index < 8; ++index)
        _children[index].GetColliding(ref checkBounds, result);
    }
    
    public void GetColliding(ref IBoundingShape checkBounds, List<T> result)
    {
      if (!_bounds.Intersects(checkBounds))
        return;
      foreach (var t in _objects)
      {
        if (t.Bounds.Intersects(checkBounds))
          result.Add(t.Obj);
      }
      if (_children == null)
        return;
      for (var index = 0; index < 8; ++index)
        _children[index].GetColliding(ref checkBounds, result);
    }

    /// <summary>
    /// Returns an array of objects that intersect with the specified ray, if any. Otherwise returns an empty array. See also: IsColliding.
    /// </summary>
    /// <param name="checkRay">Ray to check. Passing by ref as it improves performance with structs.</param>
    /// <param name="maxDistance">Distance to check.</param>
    /// <param name="result">List result.</param>
    /// <returns>Objects that intersect with the specified ray.</returns>
    public void GetColliding(ref Ray checkRay, List<T> result, float maxDistance = float.PositiveInfinity)
    {
      if (!_bounds.IntersectRay(checkRay, out var distance) || distance > (double) maxDistance)
        return;
      foreach (var t in _objects)
      {
        if (t.Bounds.IntersectRay(checkRay, out distance) && distance <= (double) maxDistance)
          result.Add(t.Obj);
      }
      if (_children == null)
        return;
      for (var index = 0; index < 8; ++index)
        _children[index].GetColliding(ref checkRay, result, maxDistance);
    }

    /// <summary>Set the 8 children of this octree.</summary>
    /// <param name="childOctrees">The 8 new child nodes.</param>
    public void SetChildren(Node[] childOctrees)
    {
      _children = childOctrees.Length == 8 ? childOctrees : throw new ArgumentException("Child octree array must be length 8. Was length: " + childOctrees.Length, nameof (childOctrees));
    }

    /// <summary>
    /// We can shrink the octree if:
    /// - This node is &gt;= double minLength in length
    /// - All objects in the root node are within one octant
    /// - This node doesn't have children, or does but 7/8 children are empty
    /// We can also shrink it if there are no objects left at all!
    /// </summary>
    /// <param name="minLength">Minimum dimensions of a node in this octree.</param>
    /// <returns>The new root, or the existing one if we didn't shrink.</returns>
    public Node ShrinkIfPossible(float minLength)
    {
      if (BaseLength < 2.0 * minLength || _objects.Count == 0 && (_children == null || _children.Length == 0))
        return this;
      var index1 = -1;
      for (var index2 = 0; index2 < _objects.Count; ++index2)
      {
        var octreeObject = _objects[index2];
        var index3 = BestFitChild(octreeObject.Bounds.Center);
        if (index2 != 0 && index3 != index1 || !Encapsulates(_childBounds[index3], octreeObject.Bounds))
          return this;
        if (index1 < 0)
          index1 = index3;
      }
      if (_children != null)
      {
        var flag = false;
        for (var index4 = 0; index4 < _children.Length; ++index4)
        {
          if (!_children[index4].HasAnyObjects()) continue;
          if (flag || index1 >= 0 && index1 != index4)
            return this;
          flag = true;
          index1 = index4;
        }
      }

      if (_children != null) return index1 == -1 ? this : _children[index1];
      SetValues(BaseLength / 2f, _minSize, _looseness, _childBounds[index1].Center);
      return this;
    }

    /// <summary>
    /// Find which child node this object would be most likely to fit in.
    /// </summary>
    /// <param name="objBoundsCenter">The object's bounds center.</param>
    /// <returns>One of the eight child octants.</returns>
    public int BestFitChild(Vector3 objBoundsCenter)
    {
      return (objBoundsCenter.X <= (double) Center.X ? 0 : 1) + (objBoundsCenter.Y >= (double) Center.Y ? 0 : 4) + (objBoundsCenter.Z <= (double) Center.Z ? 0 : 2);
    }

    /// <summary>
    /// Checks if this node or anything below it has something in it.
    /// </summary>
    /// <returns>True if this node or any of its children, grandchildren etc have something in them</returns>
    public bool HasAnyObjects()
    {
      if (_objects.Count > 0)
        return true;
      if (_children == null) return false;
      for (var index = 0; index < 8; ++index)
      {
        if (_children[index].HasAnyObjects())
          return true;
      }
      return false;
    }

    /// <summary>Set values for this node.</summary>
    /// <param name="baseLengthVal">Length of this node, not taking looseness into account.</param>
    /// <param name="minSizeVal">Minimum size of nodes in this octree.</param>
    /// <param name="loosenessVal">Multiplier for baseLengthVal to get the actual size.</param>
    /// <param name="centerVal">Center position of this node.</param>
    private void SetValues(
      float baseLengthVal,
      float minSizeVal,
      float loosenessVal,
      Vector3 centerVal)
    {
      BaseLength = baseLengthVal;
      _minSize = minSizeVal;
      _looseness = loosenessVal;
      Center = centerVal;
      _adjLength = _looseness * baseLengthVal;
      _bounds = new BoundingBox(Center, new Vector3(_adjLength, _adjLength, _adjLength));
      var num1 = BaseLength / 4f;
      var num2 = BaseLength / 2f * _looseness;
      var size = new Vector3(num2, num2, num2);
      _childBounds = new BoundingBox[8];
      _childBounds[0] = new BoundingBox(Center + new Vector3(-num1, num1, -num1), size);
      _childBounds[1] = new BoundingBox(Center + new Vector3(num1, num1, -num1), size);
      _childBounds[2] = new BoundingBox(Center + new Vector3(-num1, num1, num1), size);
      _childBounds[3] = new BoundingBox(Center + new Vector3(num1, num1, num1), size);
      _childBounds[4] = new BoundingBox(Center + new Vector3(-num1, -num1, -num1), size);
      _childBounds[5] = new BoundingBox(Center + new Vector3(num1, -num1, -num1), size);
      _childBounds[6] = new BoundingBox(Center + new Vector3(-num1, -num1, num1), size);
      _childBounds[7] = new BoundingBox(Center + new Vector3(num1, -num1, num1), size);
    }

    /// <summary>Private counterpart to the public Add method.</summary>
    /// <param name="obj">Object to add.</param>
    /// <param name="objBounds">3D bounding box around the object.</param>
    private void SubAdd(T obj, BoundingBox objBounds)
    {
      if (!HasChildren)
      {
        if (_objects.Count < 8 || BaseLength / 2.0 < _minSize)
        {
          _objects.Add(new OctreeObject
          {
            Obj = obj,
            Bounds = objBounds
          });
          return;
        }
        if (_children == null)
        {
          Split();
          if (_children == null)
            throw new InvalidOperationException("Child creation failed for an unknown reason. Early exit.");
          for (var index1 = _objects.Count - 1; index1 >= 0; --index1)
          {
            var octreeObject = _objects[index1];
            var index2 = BestFitChild(octreeObject.Bounds.Center);
            if (!Encapsulates(_children[index2]._bounds, octreeObject.Bounds)) continue;
            _children[index2].SubAdd(octreeObject.Obj, octreeObject.Bounds);
            _objects.Remove(octreeObject);
          }
        }
      }
      var index = BestFitChild(objBounds.Center);
      if (Encapsulates(_children[index]._bounds, objBounds))
        _children[index].SubAdd(obj, objBounds);
      else
        _objects.Add(new OctreeObject
        {
          Obj = obj,
          Bounds = objBounds
        });
    }

    /// <summary>
    /// Private counterpart to the public <see cref="M:Octree.BoundsOctree`1.Node.Remove(`0,Octree.BoundingBox)" /> method.
    /// </summary>
    /// <param name="obj">Object to remove.</param>
    /// <param name="objBounds">3D bounding box around the object.</param>
    /// <returns>True if the object was removed successfully.</returns>
    private bool SubRemove(T obj, BoundingBox objBounds)
    {
      var flag = false;
      for (var index = 0; index < _objects.Count; ++index)
      {
        if (!_objects[index].Obj.Equals(obj)) continue;
        flag = _objects.Remove(_objects[index]);
        break;
      }
      if (!flag && _children != null)
        flag = _children[BestFitChild(objBounds.Center)].SubRemove(obj, objBounds);
      if (flag && _children != null && ShouldMerge())
        Merge();
      return flag;
    }

    /// <summary>Splits the octree into eight children.</summary>
    private void Split()
    {
      var num = BaseLength / 4f;
      var baseLengthVal = BaseLength / 2f;
      _children = new Node[8];
      _children[0] = new Node(baseLengthVal, _minSize, _looseness, Center + new Vector3(-num, num, -num));
      _children[1] = new Node(baseLengthVal, _minSize, _looseness, Center + new Vector3(num, num, -num));
      _children[2] = new Node(baseLengthVal, _minSize, _looseness, Center + new Vector3(-num, num, num));
      _children[3] = new Node(baseLengthVal, _minSize, _looseness, Center + new Vector3(num, num, num));
      _children[4] = new Node(baseLengthVal, _minSize, _looseness, Center + new Vector3(-num, -num, -num));
      _children[5] = new Node(baseLengthVal, _minSize, _looseness, Center + new Vector3(num, -num, -num));
      _children[6] = new Node(baseLengthVal, _minSize, _looseness, Center + new Vector3(-num, -num, num));
      _children[7] = new Node(baseLengthVal, _minSize, _looseness, Center + new Vector3(num, -num, num));
    }

    /// <summary>
    /// Merge all children into this node - the opposite of Split.
    /// Note: We only have to check one level down since a merge will never happen if the children already have children,
    /// since THAT won't happen unless there are already too many objects to merge.
    /// </summary>
    private void Merge()
    {
      for (var index1 = 0; index1 < 8; ++index1)
      {
        var child = _children[index1];
        for (var index2 = child._objects.Count - 1; index2 >= 0; --index2)
          _objects.Add(child._objects[index2]);
      }
      _children = null;
    }

    /// <summary>Checks if outerBounds encapsulates innerBounds.</summary>
    /// <param name="outerBounds">Outer bounds.</param>
    /// <param name="innerBounds">Inner bounds.</param>
    /// <returns>True if innerBounds is fully encapsulated by outerBounds.</returns>
    private static bool Encapsulates(BoundingBox outerBounds, BoundingBox innerBounds)
    {
      return outerBounds.Contains(innerBounds.Min) && outerBounds.Contains(innerBounds.Max);
    }

    /// <summary>
    /// Checks if there are few enough objects in this node and its children that the children should all be merged into this.
    /// </summary>
    /// <returns>True there are less or the same amount of objects in this and its children than <see cref="F:Octree.BoundsOctree`1.Node.NumObjectsAllowed" />.</returns>
    private bool ShouldMerge()
    {
      var count = _objects.Count;
      if (_children == null) return count <= 8;
      foreach (var child in _children)
      {
        if (child._children != null)
          return false;
        count += child._objects.Count;
      }
      return count <= 8;
    }

    /// <summary>An object in the octree</summary>
    private class OctreeObject
    {
      /// <summary>Object content</summary>
      public T Obj;
      /// <summary>Object bounds</summary>
      public BoundingBox Bounds;
    }
  }
}