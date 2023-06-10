using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PlayerHUD : MonoBehaviour
{
    [SerializeField]
    private Slider healthSlider;
    [SerializeField]
    private PlayerData playerData;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (
            healthSlider != null &&
            playerData != null
        )
        {
            healthSlider.value = playerData.health;
        }
    }
}
