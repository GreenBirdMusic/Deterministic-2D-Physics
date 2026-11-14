using System;
using System.Collections.Generic;

namespace Physics;

public class GroupTable
{
    private Dictionary<string, int> groupNumbers;

    private CollisionResponse[,] collisionData;

    public GroupTable(string[] groups)
    {
        groupNumbers = new Dictionary<string, int>(groups.Length);
        collisionData = new CollisionResponse[groups.Length, groups.Length];
        for (int i = 0; i < groups.Length; i++)
        {
            if (groupNumbers.ContainsKey(groups[i])) {throw new ArgumentException($"Duplicate group name: {groups[i]}");}
            groupNumbers.Add(groups[i], i);
        }
    }

    public CollisionGroup GetGroup(string name)
    {
        if (!groupNumbers.TryGetValue(name, out int id)) {throw new ArgumentException($"Group {name} does not exist.");}
        return new CollisionGroup(id);
    }

    public void Set(CollisionGroup a, CollisionGroup b, CollisionResponse r)
    {
        collisionData[a.ID, b.ID] = r;
        collisionData[b.ID, a.ID] = r;
    }

    public CollisionResponse Get(CollisionGroup a, CollisionGroup b)
    {
        return collisionData[a.ID, b.ID];
    }

    public int Count => groupNumbers.Count;
}