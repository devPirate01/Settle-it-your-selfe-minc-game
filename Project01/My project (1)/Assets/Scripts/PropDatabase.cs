using UnityEngine;

public enum PropClass
{
    Small,  // Mugs, phones, staplers (Fast throw, 1-handed)
    Medium, // Keyboards, laptops, monitors (Balanced, 1-handed)
    Heavy   // Chairs, water coolers, desks (Devastating, 2-handed, heavy)
}

public enum HoldStyle
{
    OneHanded,
    TwoHanded
}

[System.Serializable]
public class PropStats
{
    public HoldStyle holdStyle = HoldStyle.OneHanded;
    public int damage = 25;
    public float throwForce = 15f;
    [Range(0.2f, 1f)] public float moveSpeedMultiplier = 1f; // 1 = full speed, 0.7 = 30% slower
}

[CreateAssetMenu(fileName = "PropDatabase", menuName = "Game/Prop Database")]
public class PropDatabase : ScriptableObject
{
    private static PropDatabase _instance;
    public static PropDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<PropDatabase>("PropDatabase");
                if (_instance == null)
                {
                    _instance = CreateInstance<PropDatabase>();
                }
            }
            return _instance;
        }
    }

    [Header("Small Props (Mugs, Staplers, Phones)")]
    public PropStats small = new PropStats 
    { 
        holdStyle = HoldStyle.OneHanded, 
        damage = 20, 
        throwForce = 18f, 
        moveSpeedMultiplier = 1.0f 
    };

    [Header("Medium Props (Keyboards, Laptops, Monitors)")]
    public PropStats medium = new PropStats 
    { 
        holdStyle = HoldStyle.OneHanded, 
        damage = 35, 
        throwForce = 15f, 
        moveSpeedMultiplier = 1.0f 
    };

    [Header("Heavy Props (Chairs, Desks, Water Coolers)")]
    public PropStats heavy = new PropStats 
    { 
        holdStyle = HoldStyle.TwoHanded, 
        damage = 60, 
        throwForce = 6.5f, 
        moveSpeedMultiplier = 0.7f 
    };

    public PropStats GetStats(PropClass pClass)
    {
        return pClass switch
        {
            PropClass.Small => small,
            PropClass.Medium => medium,
            PropClass.Heavy => heavy,
            _ => medium
        };
    }
}
