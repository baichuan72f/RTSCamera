using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms;

public class RTSExt : CinemachineExtension
{
    CinemachineFreeLook _freeLook;
    CinemachineCameraOffset _offset;
    private bool _onDrag;
    private Vector3 _startRoundPos;
    private Vector3 _roundDis;
    private Vector3 _startRoundView;
    private Vector3 _startMovePos;
    private Vector3 _moveDis;
    private Vector3 _startMoveView;
    private CinemachineFreeLook.Orbit[] _startOrbits;
    public float moveSpeed = 100;
    float keySpeed = 200;
    float mouseSpeed = 1000;
    public float rotaSpeed = 1.2f;
    public float scaleSpeed = 10;
    public float scaleMin = 0.2f;
    public float scaleMax = 5;

    [SerializeField] float _scale = 1;
    //private float _offsetZ;

    protected override void OnEnable()
    {
        base.OnEnable();
        Init();
    }

    private void Init()
    {
        _freeLook = GetComponent<CinemachineFreeLook>();
        _offset = GetComponent<CinemachineCameraOffset>();
        if (_freeLook != null)
        {
            _freeLook.m_XAxis.m_InputAxisName = String.Empty;
            _freeLook.m_YAxis.m_InputAxisName = String.Empty;
            _scale = 1;
            _startOrbits = new CinemachineFreeLook.Orbit[_freeLook.m_Orbits.Length];
            for (int i = 0; i < _freeLook.m_Orbits.Length; i++)
            {
                _startOrbits[i] =
                    new CinemachineFreeLook.Orbit(_freeLook.m_Orbits[i].m_Height, _freeLook.m_Orbits[i].m_Radius);
            }

            Reset();
        }
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
        ref CameraState state, float deltaTime)
    {
        _freeLook = vcam as CinemachineFreeLook;
        if (!_freeLook) return;
        if (!Input.GetMouseButton(1))
        {
            //滚轮缩放
            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            {
                scaleFreeLook(deltaTime);
            }
        }

        //储存开始值
        if (Input.GetMouseButtonDown(0))
        {
            StartRound();
        }


        //执行视角围绕
        if (Input.GetMouseButton(0))
        {
            var roundPos = (Input.mousePosition + _roundDis - _startRoundPos) * rotaSpeed;
            RoundTarget(roundPos);
        }

        //储存开始值
        if (Input.GetMouseButtonDown(1))
        {
            StartMove();
        }

        //执行平移
        if (Input.GetMouseButton(1))
        {
            //滚轮缩放
            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            {
                _moveDis.z += Input.mouseScrollDelta.y * deltaTime * mouseSpeed;
            }

            if (Input.GetKey(KeyCode.W))
            {
                _moveDis.z += deltaTime * keySpeed;
            }

            if (Input.GetKey(KeyCode.S))
            {
                _moveDis.z += -deltaTime * keySpeed;
            }

            if (Input.GetKey(KeyCode.D))
            {
                _moveDis.x += deltaTime * keySpeed;
            }

            if (Input.GetKey(KeyCode.A))
            {
                _moveDis.x += -deltaTime * keySpeed;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                _moveDis.y += deltaTime * keySpeed;
            }

            if (Input.GetKey(KeyCode.E))
            {
                _moveDis.y += -deltaTime * keySpeed;
            }

            var moveDistance = (Input.mousePosition + _moveDis - _startMovePos) * moveSpeed;
            MoveXY(moveDistance);
        }

        //储存开始值
        if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
        {
            Reset();
        }
    }

    private void StartRound()
    {
        _startRoundPos = Input.mousePosition;
        _roundDis = Vector3.zero;
        _startRoundView = new Vector2(_freeLook.m_XAxis.Value, _freeLook.m_YAxis.Value);
    }

    private void RoundTarget(Vector3 roundPos)
    {
        _freeLook.m_XAxis.Value = _startRoundView.x + (roundPos.x * 360 / Screen.width);
        _freeLook.m_YAxis.Value = _startRoundView.y + (roundPos.y / Screen.height);
    }

    private void StartMove()
    {
        _startMovePos = Input.mousePosition;
        _moveDis = Vector3.zero;
        _startMoveView = _offset.m_Offset;
        //_offsetZ = 0;
    }

    private void MoveXY(Vector3 moveDistance)
    {
        moveDistance = new Vector3(moveDistance.x / Screen.width, moveDistance.y / Screen.height,
            moveDistance.z / (Screen.width > Screen.height ? Screen.width : Screen.height));
        _offset.m_Offset = _startMoveView + moveDistance * _scale;
    }

    private void Reset()
    {
        _startRoundPos = Input.mousePosition;
        _startRoundView = new Vector2(_freeLook.m_XAxis.Value, _freeLook.m_YAxis.Value);
        _roundDis = Vector3.zero;

        _startMovePos = Input.mousePosition;
        _startMoveView = Vector3.zero;
        _moveDis = Vector3.zero;

        _offset.m_Offset = Vector3.zero;
    }

    private void scaleFreeLook(float deltaTime)
    {
        var add = Input.mouseScrollDelta.y * deltaTime * scaleSpeed;
        if (Math.Abs(add) <= 0) return;
        //合规判断
        var s = _scale + add;
        if (s <= 0) return;
        var targetScale = ScaleValue(s);
        if (float.IsNaN(targetScale)) return;

        // 是否可以缩小
        bool canMin = scaleMax > _scale && s < _scale;
        // 是否可以扩大
        bool canMax = scaleMin < _scale && _scale > s;
        bool canMove = (scaleMin < s && s < scaleMax) || canMin || canMax;
        _scale = Mathf.Clamp(s, scaleMin, scaleMax);
        if (canMove)
        {
            for (int i = 0; i < _freeLook.m_Orbits.Length; i++)
            {
                _freeLook.m_Orbits[i].m_Radius = _startOrbits[i].m_Radius * _scale;
                _freeLook.m_Orbits[i].m_Height = _startOrbits[i].m_Height * _scale;
            }
        }
    }

    float ScaleValue(float before)
    {
        return Mathf.Pow(before, 0.5f);
    }
}