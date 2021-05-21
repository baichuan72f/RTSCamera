using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(CinemachineFreeLook)), RequireComponent(typeof(CinemachineCameraOffset))]
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
    float _scale = 1;

    [Tooltip("The volume within which the camera is to be contained")]
    public Collider m_BoundingVolume;

    public bool cannotMoveAfterCollision = true;
    int _clampState = 0;
    int _nearFarState = 0;

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

        bool startKey = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) ||
                        Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E);

        //储存开始值
        if (Input.GetMouseButtonDown(1) || startKey)
        {
            StartMove();
        }

        bool keyMove = false;
        if (Input.GetKey(KeyCode.W))
        {
            _moveDis.z += deltaTime * keySpeed;
            keyMove = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            _moveDis.z += -deltaTime * keySpeed;
            keyMove = true;
        }

        if (Input.GetKey(KeyCode.D))
        {
            _moveDis.x += deltaTime * keySpeed;
            keyMove = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            _moveDis.x += -deltaTime * keySpeed;
            keyMove = true;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            _moveDis.y += deltaTime * keySpeed;
            keyMove = true;
        }

        if (Input.GetKey(KeyCode.E))
        {
            _moveDis.y += -deltaTime * keySpeed;
            keyMove = true;
        }

        if (m_BoundingVolume)
        {
            ColliderClamp(vcam, stage, ref state, deltaTime);
        }

        //执行平移
        if (Input.GetMouseButton(1) || keyMove)
        {
            //滚轮缩放
            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            {
                _moveDis.z += Input.mouseScrollDelta.y * deltaTime * mouseSpeed;
            }

            var moveDistance = (Input.mousePosition + _moveDis - _startMovePos) * moveSpeed;
            MoveXY(ref state, moveDistance);
        }


        //储存开始值
        if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
        {
            Reset();
        }
    }


    void ColliderClamp(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
        ref CameraState state, float deltaTime)
    {
        var camPos = state.CorrectedPosition;
        Vector3 displacement = m_BoundingVolume.ClosestPoint(camPos) - camPos;
        state.PositionCorrection += displacement;

        //接触边界的状态
        _clampState = displacement.magnitude.CompareTo(0);
        if (!cannotMoveAfterCollision)
        {
            _clampState = 0;
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

    private void MoveXY(ref CameraState state, Vector3 moveDistance)
    {
        moveDistance = new Vector3(moveDistance.x / Screen.width, moveDistance.y / Screen.height,
            moveDistance.z / (Screen.width > Screen.height ? Screen.width : Screen.height));
        var before = _startMoveView;
        var after = _startMoveView + moveDistance * _scale;
        var m = before.magnitude.CompareTo(after.magnitude);
        if (m != 0) _nearFarState = m;
        //Debug.Log("_clamp:" + _clampState + " _moveState:" + _nearFarState);
        if (_clampState != 1 || _nearFarState != -1)
        {
            _offset.m_Offset = _startMoveView + moveDistance * _scale;
        }
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

    class VcamExtraState
    {
        public Vector3 m_previousDisplacement;
        public float confinerDisplacement;
    };
}