using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraGroup : MonoBehaviour
{
    public CinemachineVirtualCameraBase[] cameras;
    public CinemachineVirtualCameraBase CurrentCamera { get; private set; }

    public int index_c = 0;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            index_c++;
            if (index_c >= cameras.Length)
            {
                index_c = 0;
            }

            index_c = Mathf.Clamp(index_c, 0, cameras.Length - 1);
            ChangeCameraStates(index_c, true);
        }
    }

    public void ChangeCameraStates(int idx, bool activeGameObject = false)
    {
        if (cameras != null && 0 <= idx && idx < cameras.Length)
        {
            ChangeCameraStates(cameras[idx], activeGameObject);
        }
    }

    public void ChangeCameraStates(CinemachineVirtualCameraBase state, bool activeGameObject = false)
    {
        //合法校验
        if (state == null) return;
        //激活当前相机
        CurrentCamera = state;
        if (activeGameObject) CurrentCamera.gameObject.SetActive(true);
        CurrentCamera.Priority = 1;
        //隐藏其他相机
        if (cameras != null)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].Priority = (cameras[i] == state) ? 1 : 0;
                if (activeGameObject)
                {
                    cameras[i].gameObject.SetActive(cameras[i] == state);
                }
            }
        }
    }

    public void LookTo(Transform target, CinemachineVirtualCameraBase virtualCamera = null)
    {
        if (virtualCamera == null)
        {
            if (CurrentCamera == null && cameras != null && cameras.Length > 0)
            {
                CurrentCamera = cameras[0];
            }

            virtualCamera = CurrentCamera;
        }

        if (virtualCamera == null) return;
        virtualCamera.LookAt = target;
        virtualCamera.Follow = target;
    }
}