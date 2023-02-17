using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        Application.targetFrameRate = 60;

    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }


}
