using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraGroup : MonoBehaviour
{
    public CinemachineVirtualCameraBase[] cameras;

    public int index = 0;

    private CinemachineVirtualCameraBase _currentVirtualCamera;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            index++;
            if (index >= cameras.Length)
            {
                index = 0;
            }

            index = Mathf.Clamp(index, 0, cameras.Length - 1);
            ChangeCameraStates(index);
        }
    }

    public void ChangeCameraStates(int idx)
    {
        if (cameras != null)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].gameObject.SetActive(idx == i);
            }

            if (0 <= idx && idx < cameras.Length)
            {
                _currentVirtualCamera = cameras[idx];
            }
        }
    }

    public void LookTo(Transform target)
    {
        _currentVirtualCamera.LookAt = target;
        _currentVirtualCamera.Follow = target;
    }
}