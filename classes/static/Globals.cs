using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Globals;

public interface IGameObject { }
public static class StringExtensions
{
    public static string RemovePath(this string path)
    {
        return path.Substring(path.RFind("/") + 1);
    }
    public static string RemoveFileType(this string path)
    {
        int index = path.LastIndexOf('.');
        return path.Substring(0, index);
    }
    public static string RemovePathAndFileType(this string path)
    {
        return path.RemovePath().RemoveFileType();
    }
}

public static class IGameObjectExtensions
{
    public static Node AsNode(this IGameObject gameObject)
    {
        return gameObject as Node;
    }
}

public static class LayerExtensions
{
    /// <summary>
    /// Converts a layer to its corrisponding bit value. Example BlockLight -> 512
    /// </summary>
    public static uint ConvertToBitMask(this PhysicsLayer layer)
    {
        return (uint)Mathf.Pow(2, (uint)layer - 1);
    }

    /// <summary>
    /// Returns true if a collision layer is active on obj.
    /// </summary>
    public static bool CollisionLayerActive(this PhysicsLayer number, CollisionObject3D obj)
    {
        return obj.GetCollisionLayerValue((int)number);
    }

    public static bool CollisionMaskActive(this PhysicsLayer number, CollisionObject3D obj)
    {
        return obj.GetCollisionMaskValue((int)number);
    }
}

public static class NodeExtensions
{
    #region Meta Accessors

    public static uint GetUniqueId(this Node node)
    {
        return uint.Parse(node.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).AsString());
    }
    public static string GetUniqueIdToString(this Node node)
    {
        return node.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).AsString();
    }
    public static ulong GetOwnerId(this Node node)
    {
        return ulong.Parse(node.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).AsString());
    }
    public static string GetOwnerIdToString(this Node node)
    {
        return node.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).AsString();
    }

    #endregion

    public static bool IsServerNode(this Node node) 
    {
        return uint.Parse(node.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString()) < NetworkDataManager.FirstAvailableSelfUniqueId;

    }
    public static bool IsValid<T>(this T node) where T : GodotObject
    {
        return GodotObject.IsInstanceValid(node) && node != null;
    }
    /// <summary>
    /// Works for any node in a level, and players which have a LevelPartitionName meta.
    /// </summary>
    /// <returns></returns>
    public static string GetLevelName(this Node node)
    {
        if (node.HasMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]))
        {
            return node.GetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]).ToString();
        }
        node.GetLevel();
        if(node == null)
        {
            return String.Empty;
        }
        return node.Name;
    }
    public static Node GetLevel(this Node node)
    {
        if(node is Player p)
        {
            string n = p.GetLevelName();
            foreach(Node no in Main.Instance.GetChildren())
            {
                if (no.Name == n)
                    return no as Level;
            }
        }
        return CheckParent(node);
    }


    private static Level CheckParent(Node node)
    {
        if (node == node.GetTree().Root)
            return null;
        if (node is Level l)
            return l;
        return CheckParent(node.GetParent());
    }

    public static bool IsOwnedBy(this Node node, Node potentialOwner)
    {
        Node temp = node;
        while(temp != potentialOwner.GetTree().Root)
        {
            if (temp.GetParent() == potentialOwner)
                return true;
            temp = node.GetParent();
        }
        return false;
    }
    /// <summary>
    /// Returns true if node is owned by the local machine. Expensive call, should be called in ready and stored in local
    /// bool values.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static bool IsLocalOwned(this Node node)
    {
        Player p;
        if (node is Player player)
            p = player;
        else
            p = node.FindParentOfType<Player>();
        if (p.IsValid())
        {
            ulong meta = ulong.Parse(p.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).ToString());
            if (meta == NetworkManager.Instance.PlayerId)
                return true;

            return false;

        }
        else if(NetworkManager.Instance.IsServer)
            return true;
        else
        {
            uint meta = uint.Parse(p.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString());
            if (meta >= NetworkDataManager.FirstAvailableSelfUniqueId)
                return true;
        }

        return false;
    }

    public static IGameObject GetParentGameObject(this Node node)
    {
        while (node != node.GetTree().Root)
        {
            node = node.GetParent();
            if (node is IGameObject i)
                return i;
            node = node.GetParent();
        }
        return null;
    }

    public static T FindParentOfType<T>(this Node node)
    {
        return FindParentOfTypeHelper<T>(node.GetParent());
    }


    private static T FindParentOfTypeHelper<T>(this Node node)
    {
        if (node == null)
            return default(T);
        if (node is T)
        {
            return (T)(object)node;
        }
        else if (node == node.GetTree().Root)
        {
            return default(T);
        }
        else
        {
            return FindParentOfTypeHelper<T>(node.GetParent());
        }
    }

    public static bool Owns(this Node node, Node potentiallyOwned)
    {
        return potentiallyOwned.IsOwnedBy(node);
    }

    public static T IfValid<T>(this T node) where T : GodotObject
        => node.IsValid() ? node : null;

    /// <summary>
    /// Function for searching for child node of Type T. Removes need for searching for a
    /// specific name of a node, reducing potential errors in name checking being inaccurate.
    /// Supports checking 5 layers of nodes. This method is ineffecient, and should never be used repetitively 
    /// in _process.
    /// </summary>
    /// <returns>First instance of Type T</returns>
    public static T GetChildOfType<T>(this Node node)
    {
        if (node == null)
            return default(T);

        foreach (Node child in node.GetChildren())
            if (child is T)
                return (T)(object)child;

        return default(T);
    }

    /// <summary>
    /// Function for searching for children nodes of Type T.
    /// </summary>
    /// <returns>List of all instances of Type T</returns>
    public static List<T> GetChildrenOfType<T>(this Node node)
    {
        List<T> list = new List<T>();
        if (node == null)
            return list;

        foreach (Node child in node.GetChildren())
            if (child is T)
                list.Add((T)(object)child);

        return list;
    }

    /// <summary>
    /// Function for searching for children nodes of Type T.
    /// </summary>
    /// <returns>List of all instances of Type T that are children or lower.</returns>
    public static List<T> GetAllChildrenOfType<T>(this Node node)
    {
        List<T> list = new List<T>();
        list.AddRange(GetChildrenOfType<T>(node));
        for (int i = node.GetChildCount() - 1; i >= 0; i--)
        //foreach (Node child in node.GetChildren())
        {
            list.AddRange(GetAllChildrenOfType<T>(node.GetChild(i)));
        }

        return list;
    }

    /// <summary>
    /// Function for searching for sibling node of Type T. Removes need for searching for a
    /// specific name of a node, reducing potential errors in name checking being inaccurate.
    /// </summary>
    /// <returns>First instance of Type T</returns>
    public static T GetSiblingOfType<T>(this Node node)
    {
        return node.GetParent().GetChildOfType<T>();
    }
    /// <summary>
    /// Function for searching for sibling nodes of Type T.
    /// </summary>
    /// <returns>List of all instances of Type T</returns>
    public static List<T> GetSiblingsOfType<T>(this Node node)
    {
        return node.GetParent().GetChildrenOfType<T>();
    }

}

public class ConsoleAttribute : Attribute
{

}

public static class Globals
{
    /// <summary>
    /// MAJOR NOTE: Any modification to these must be reflected in the SNC.Groups
    /// </summary>
    public enum Groups
    {
        AutoLoad,
        SelfOnly,
        NotPersistent,
        IgnoreChildren, // both save and network
        IgnoreChildrenSave,
        IgnoreChildrenNetwork,
        Outside,
        Level
    }
    /// <summary>
    /// MAJOR NOTE: Any modification to these must be reflected in the SNC.Meta
    /// </summary>
    public enum Meta
    {
        UniqueId,
        OwnerId,
        LevelPartitionName,
        UniqueItemId
    }
    public enum DataType
    {
        RpcCall,
        ClientInputUpdate,
        ServerUpdate,
        FullServerData,
        ServerAdd,
        ServerRemove,
        RequestForceUpdate
    }

    public enum PhysicsLayer
    {
        Neutral = 1,
        Player,
        Enemy,
        Interaction,
        BlockLightBody,
        BlockLightArea,
        Ground,



        VirtualMouse = 31,
        Clickable,
        NULL = 33
    }

        /// <summary>
       /// Returns true if every layer of mask2 is also on mask.
       /// </summary>
    public static bool LayersUnion(uint mask, uint mask2)
    {
        if (mask == mask2) return true;

        for (int i = 0; i < 32; i++)
        {
            if (((mask2 >> i) & 1) == 1)
            {
                if (((mask >> i) & 1) == 0)
                {
                    return false;
                }
            }
        }

        return true;
    }
    /// <summary>
    /// Returns true if any layer of mask2 is also on mask
    /// </summary>
    public static bool LayersIntersect(uint mask, uint mask2)
    {
        if (mask == mask2) return true;

        for (int i = 0; i < 32; i++)
        {
            if (((mask2 >> i) & 1) == 1)
            {
                if (((mask >> i) & 1) == 1)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string RemoveNamespace(string name)
    {
        int index = name.RFind(".");
        if (index < 0)
            return name;
        else
            return name.Substring(index + 1, name.Length - (index + 1));
    }
}