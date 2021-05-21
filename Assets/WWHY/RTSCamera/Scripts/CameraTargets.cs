using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraTargets : MonoBehaviour
{

    public int index_g = 0;
    public CinemachineVirtualCameraBase virtualCamera;

    public Transform[] targets;

    // Start is called before the first frame update
    void Start()
    {
        if (virtualCamera == null) virtualCamera = GetComponentInChildren<CinemachineVirtualCameraBase>();
        if (targets == null || targets.Length == 0) targets = transform.GetComponentsInChildren<Transform>();
    }

    // Update is called once per frame
    void Update()
    {


        if (Input.GetKeyDown(KeyCode.T) && targets != null)
        {
            index_g++;
            if (index_g >= targets.Length)
            {
                index_g = 0;
            }

            index_g = Mathf.Clamp(index_g, 0, targets.Length - 1);
            virtualCamera.LookAt = targets[index_g];
            virtualCamera.Follow = targets[index_g];
        }
    }
}