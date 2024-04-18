using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum NODESTATE
{
    RUNNING,
    SUCCESS,
    FAILURE
}

public class Node
{
    protected NODESTATE state;

    public Node parent;
    public List<Node> children = new List<Node>();

    private Dictionary<string, object> _dataContext = new Dictionary<string, object>();

    public Node()
    {
        parent = null;
    }
    public Node(List<Node> children)
    {
        foreach (Node child in children)
            _Attach(child);
    }

    private void _Attach(Node node)
    {
        node.parent = this;
        children.Add(node);
    }

    public virtual NODESTATE Evaluate() => NODESTATE.FAILURE;

    public void SetData(string key, object value)
    {
        _dataContext[key] = value;
    }

    public object GetData(string key)
    {
        object value = null;
        if (_dataContext.TryGetValue(key, out value))
            return value;

        Node node = parent;
        while (node != null)
        {
            value = node.GetData(key);
            if (value != null)
                return value;
            node = node.parent;
        }
        return null;
    }

    public bool ClearData(string key)
    {
        if (_dataContext.ContainsKey(key))
        {
            _dataContext.Remove(key);
            return true;
        }

        Node node = parent;
        while (node != null)
        {
            bool cleared = node.ClearData(key);
            if (cleared)
                return true;
            node = node.parent;
        }
        return false;
    }

    //public void StopTask()
    //{
    //    state = NODESTATE.SUCCESS;
    //}
}
