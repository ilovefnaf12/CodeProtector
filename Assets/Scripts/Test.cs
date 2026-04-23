using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private int privateField1 = 100;
    public int publicField1 = 100;

    private void PrivateFunction1()
    {
        print("Private Function 1");
    }

    private void PublicFunction1()
    {
        print("Public Function 1");
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
