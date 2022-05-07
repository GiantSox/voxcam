using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testshift : MonoBehaviour
{
    public static Vector2 center = Vector2.zero;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var rt = gameObject.GetComponent<RectTransform>();
        rt.anchoredPosition = center;
    }
}
