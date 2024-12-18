using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapRingItem : MonoBehaviour
{
    public Image selfImg;
    public Image img;
    public RectTransform promptRot;
    public RectTransform promptInRing;
    public bool prompt;
    public bool promptRing;

    void Update()
    {
        promptRot.gameObject.SetActive(prompt);
        promptInRing.gameObject.SetActive(promptRing);
    }
}
