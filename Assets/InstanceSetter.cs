using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceSetter : MonoBehaviour
{
    public int dropdown;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeInstance(int option)
    {
        if (option != 0)
            CVRPMain.inst.assets[dropdown] = InstanceLoader.inst.textAssets[option - 1];
        else
            CVRPMain.inst.assets[dropdown] = null;
    }
}
