using System;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WWHY.RTSCamera
{
    [RequireComponent(typeof(CinemachineFreeLook))]
    public class RtsExt : CinemachineExtension
    {
        CinemachineFreeLook _freeLook; //基础相机
        private Vector3 _startFollow; // 初始目标点     
        private Vector3 _startRoundPos; //初始旋绕时相机位置
        private Vector3 _startRoundInput; //初始旋绕时_freeLook组件的值
        private Vector3 _startMovePos; //初始移动时相机位置
        private Vector3 _moveDistance; //相机移动距离
        private CinemachineFreeLook.Orbit[] _startOrbits; //_freeLook的三个Rig初始状态

        public float keyMoveSpeed = 1f; //键盘移动系数
        public float mouseMoveSpeed = 2f; //鼠标移动系数

        float _scale = 1; //相机的操作比例（移动旋转缩放）
        int _clampState = 0; //是否接触到边界
        int _nearFarState = 0; //相机接近或远离范围中心
        KeyCode[] _keyCodes;

        private Vector3[] _disArr = new Vector3[]
            {Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back};

        public float moveSpeed = 0.05f; //移动速度
        public float rotaSpeed = 1; //旋转速度
        public float deltaSpeed = 1; //鼠标缩放系数

        [Tooltip("Zhe range of Scale, x = minRange,y = maxRange")]
        public Vector2 scaleRange = new Vector2(0.1f, 10);

        [Tooltip("Disable On UI ?")] public bool disableOnUi = false;

        [Tooltip("The volume within which the camera is to be contained")]
        public Collider mBoundingVolume;

        [Tooltip("after Collision is can not move ?")]
        public bool cannotMoveAfterCollision = true;


        KeyCode upKey = KeyCode.Q;
        KeyCode downKey = KeyCode.E;
        KeyCode leftKey = KeyCode.A;
        KeyCode rightKey = KeyCode.D;
        KeyCode forwardKey = KeyCode.W;
        KeyCode backKey = KeyCode.S;

        //初始化
        void Start()
        {
            _freeLook = GetComponent<CinemachineFreeLook>();
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

                _startFollow = _freeLook.Follow.position;
                _keyCodes = new KeyCode[] {upKey, downKey, leftKey, rightKey, forwardKey, backKey};
            }
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state, float deltaTime)
        {
            if (_freeLook != null && _freeLook.Follow == null) return;
            //储存开始值
            if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
            {
                _freeLook.Follow.position = Vector3.zero;
                Reset();
                return;
            }

            //在UI上失效
            if (disableOnUi && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log(1);
                return;
            }

            if (!Input.GetMouseButton(1))
            {
                //滚轮缩放
                if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
                {
                    ScaleFreeLook(deltaTime);
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
                var roundPos = (Input.mousePosition - _startRoundPos) * rotaSpeed;
                RoundTarget(roundPos);
            }

            bool startKey = _keyCodes.Aggregate(false, (current, t) => current | Input.GetKeyDown(t));

            //储存开始值
            if (Input.GetMouseButtonDown(1) || startKey)
            {
                StartMove();
            }

            //相机移动输入
            Vector3 moveInput = Vector3.zero;
            for (var i = 0; i < _keyCodes.Length; i++)
            {
                if (Input.GetKey(_keyCodes[i])) moveInput += _disArr[i];
            }

            //如果有相机范围则限制移动
            if (mBoundingVolume)
            {
                ColliderClamp(ref state);
            }

            bool moveKey = _keyCodes.Aggregate(false, (current, t) => current | Input.GetKey(t));

            //执行平移
            if (Input.GetMouseButton(1) || moveKey)
            {
                if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
                {
                    moveInput.z += Input.mouseScrollDelta.y * mouseMoveSpeed * deltaSpeed;
                }

                MoveXy(deltaTime, state, moveInput);
            }
        }


        void ColliderClamp(ref CameraState state)
        {
            var camPos = state.CorrectedPosition;
            Vector3 displacement = mBoundingVolume.ClosestPoint(camPos) - camPos;
            state.PositionCorrection += displacement;

            //接触边界的状态
            _clampState = displacement.magnitude.CompareTo(0);
            if (!cannotMoveAfterCollision)
            {
                //若接触边界依然可以移动，则默认没有接触到边界
                _clampState = 0;
            }
        }

        private void StartRound()
        {
            _startRoundPos = Input.mousePosition;
            _startRoundInput = new Vector2(_freeLook.m_XAxis.Value, _freeLook.m_YAxis.Value);
        }

        private void RoundTarget(Vector3 roundPos)
        {
            _freeLook.m_XAxis.Value = _startRoundInput.x + (roundPos.x * 360 / Screen.width);
            _freeLook.m_YAxis.Value = _startRoundInput.y + (roundPos.y / Screen.height);
        }

        private void StartMove()
        {
            _startMovePos = Input.mousePosition;
            _moveDistance = Vector3.zero;
        }

        private void MoveXy(float deltaTime, CameraState state, Vector3 keyMove)
        {
            //当前鼠标的移动距离
            var mouseDis = (Input.mousePosition - _startMovePos) * -mouseMoveSpeed;
            //将移动距离转化为相对于屏幕的移动比例
            var mouseMove = new Vector3(mouseDis.x / Screen.width, mouseDis.y / Screen.height,
                mouseDis.z / (Screen.width > Screen.height ? Screen.width : Screen.height));
            var before = _freeLook.Follow.position;
            //移动角度修正，确保每次移动鼠标距离相同时视野移动角度尽量一致
            var dis = (_freeLook.VirtualCameraGameObject.transform.position - before).magnitude;
            var after = before + state.FinalOrientation * (mouseMove + keyMove - _moveDistance) *
                moveSpeed * dis * deltaTime;
            var m = before.magnitude.CompareTo(after.magnitude);
            if (m != 0) _nearFarState = m;
            //Debug.Log("_clamp:" + _clampState + " _moveState:" + _nearFarState);
            //如果相机没有接触到边界且正在远离
            if (_clampState != 1 || _nearFarState != -1)
            {
                _freeLook.Follow.position = after;
            }

            _moveDistance = mouseMove;
        }

        private void Reset()
        {
            if (_freeLook != null)
            {
                _freeLook.Follow.position = _startFollow;
            }

            StartRound();
            StartMove();
            for (int i = 0; i < _startOrbits.Length; i++)
            {
                _freeLook.m_Orbits[i] =
                    new CinemachineFreeLook.Orbit(_startOrbits[i].m_Height, _startOrbits[i].m_Radius);
            }

            _scale = 1;
        }

        private void ScaleFreeLook(float deltaTime)
        {
            var add = Input.mouseScrollDelta.y * deltaTime * deltaSpeed;
            if (Math.Abs(add) <= 0) return;
            //合规判断
            var s = _scale + add;
            if (s <= 0) return;

            // 是否可以缩小
            bool canMin = scaleRange.y > _scale && s < _scale;
            // 是否可以扩大
            bool canMax = scaleRange.x < _scale && _scale > s;
            bool canMove = (scaleRange.x < s && s < scaleRange.y) || canMin || canMax;
            _scale = Mathf.Clamp(s, scaleRange.x, scaleRange.y);
            if (canMove)
            {
                for (int i = 0; i < _freeLook.m_Orbits.Length; i++)
                {
                    _freeLook.m_Orbits[i].m_Radius = _startOrbits[i].m_Radius * _scale;
                    _freeLook.m_Orbits[i].m_Height = _startOrbits[i].m_Height * _scale;
                }
            }
        }
    }
}