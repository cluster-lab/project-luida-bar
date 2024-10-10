using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator
{
    public class PointOfViewManager
    {
        readonly GameObject firstPersonCameraObject;
        public Camera firstPersonCamera { get; private set; }
        public UnityEngine.Rendering.PostProcessing.PostProcessLayer firstPersonPpl { get; private set; }

        readonly GameObject thirdPersonCameraObject;
        public Camera thirdPersonCamera { get; private set; }
        public UnityEngine.Rendering.PostProcessing.PostProcessLayer thirdPersonPpl { get; private set; }

        readonly GameObject thirdPersonCameraDistance;
        readonly GameObject thirdPersonCameraScreenPosition;
        readonly GameObject thirdPersonCameraDistanceRayTarget;
        readonly GameObject thirdPersonCameraScreenPositionRayTarget;
        readonly GameObject thirdPersonCameraRayTarget;

        Vector2 thirdPersonCameraScreenPositionTarget = new Vector2(0.5f, 0.5f); //左下が0右上が1

        readonly float defaultCameraDistance;
        readonly float nearPlane;
        readonly float farPlane;
        float nowCameraDistance = 0;
        readonly Collider[] ignoreColliders;
        readonly Action<Camera> ToFirstPersonCallback;
        readonly Action<Camera> ToThirdPersonCallback;

        readonly RaycastHit[] raycastHits = new RaycastHit[5];

        bool isFirstPerson = true;
        float defaultFieldOfView = 0;

        public PointOfViewManager(
            float cameraDistance,
            float? nearPlane,
            float? farPlane,
            GameObject firstPersonCameraObject,
            Collider[] ignoreColliders,
            Action<Camera> ToFirstPersonCallback,
            Action<Camera> ToThirdPersonCallback
        )
        {
            this.defaultCameraDistance = cameraDistance;
            this.nowCameraDistance = cameraDistance;
            this.firstPersonCameraObject = firstPersonCameraObject;
            this.firstPersonCamera = firstPersonCameraObject.GetComponent<Camera>();
            this.firstPersonPpl = firstPersonCameraObject.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
            this.ignoreColliders = ignoreColliders;
            this.ToFirstPersonCallback = ToFirstPersonCallback;
            this.ToThirdPersonCallback = ToThirdPersonCallback;

            this.firstPersonCamera.cullingMask |= (1 << 7); //CameraOnlyを追加

            var root = new GameObject("CSEmulatorThirdPersonCameraRoot");
            root.transform.SetParent(firstPersonCameraObject.transform, false);
            root.transform.localPosition = Vector3.zero;

            thirdPersonCameraDistance = new GameObject("Distance");
            thirdPersonCameraDistance.transform.SetParent(root.transform, false);
            thirdPersonCameraDistance.transform.localPosition = Vector3.zero;

            thirdPersonCameraScreenPosition = new GameObject("ScreenPosition");
            thirdPersonCameraScreenPosition.transform.SetParent(thirdPersonCameraDistance.transform, false);
            thirdPersonCameraScreenPosition.transform.localPosition = Vector3.zero;

            thirdPersonCameraObject = new GameObject("CSEmulatorThirdPersonCamera");
            thirdPersonCamera = thirdPersonCameraObject.AddComponent<Camera>();
            thirdPersonCameraObject.transform.SetParent(thirdPersonCameraScreenPosition.transform, false);
            thirdPersonCamera.enabled = false;

            thirdPersonCameraDistanceRayTarget = new GameObject("DistanceRayTarget");
            thirdPersonCameraDistanceRayTarget.transform.SetParent(root.transform, false);
            thirdPersonCameraDistanceRayTarget.transform.localPosition = Vector3.zero;

            thirdPersonCameraScreenPositionRayTarget = new GameObject("ScreenPosition");
            thirdPersonCameraScreenPositionRayTarget.transform.SetParent(thirdPersonCameraDistanceRayTarget.transform, false);
            thirdPersonCameraScreenPositionRayTarget.transform.localPosition = Vector3.zero;

            thirdPersonCameraRayTarget = new GameObject("RayTarget");
            thirdPersonCameraRayTarget.transform.SetParent(thirdPersonCameraScreenPositionRayTarget.transform, false);
            thirdPersonCameraRayTarget.transform.localPosition = new Vector3(0, 0, -1);

            thirdPersonCamera.CopyFrom(firstPersonCamera);
            SetThirdPersonCameraDistance(cameraDistance);
            SetThirdPersonCameraRayTargetDistance(cameraDistance);

            thirdPersonPpl = thirdPersonCameraObject.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
            InitPostProcessLayer(thirdPersonPpl);
            thirdPersonPpl.volumeLayer = (1 << 21); //PostProcessing
            thirdPersonPpl.volumeTrigger = thirdPersonCameraObject.transform;

            defaultFieldOfView = firstPersonCamera.fieldOfView;

            if (nearPlane.HasValue)
            {
                firstPersonCamera.nearClipPlane = nearPlane.Value;
                thirdPersonCamera.nearClipPlane = nearPlane.Value;
            }
            if (farPlane.HasValue)
            {
                firstPersonCamera.farClipPlane = farPlane.Value;
                thirdPersonCamera.farClipPlane = farPlane.Value;
            }
        }
        void InitPostProcessLayer(UnityEngine.Rendering.PostProcessing.PostProcessLayer postProcessLayer)
        {
#if UNITY_EDITOR            
            var resources = UnityEditor.AssetDatabase.FindAssets("t:PostProcessResources");
            string resourcesPath = UnityEditor.AssetDatabase.GUIDToAssetPath(resources[0]);
            postProcessLayer.Init(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>(resourcesPath));
#endif
        }
        void SetThirdPersonCameraDistance(float distance)
        {
            thirdPersonCameraDistance.transform.localScale = new Vector3(distance, distance, distance);
            thirdPersonCameraObject.transform.localPosition = new Vector3(0, 0, -1);
        }
        void SetThirdPersonCameraRayTargetDistance(float distance)
        {
            thirdPersonCameraDistanceRayTarget.transform.localScale = new Vector3(distance, distance, distance);
        }

        public Vector3 GetCameraPosition()
        {
            if (isFirstPerson)
            {
                return firstPersonCameraObject.transform.position;
            }
            else
            {
                return thirdPersonCameraObject.transform.position;
            }
        }
        public Quaternion GetCameraRotation()
        {
            if (isFirstPerson)
            {
                return firstPersonCameraObject.transform.rotation;
            }
            else
            {
                return thirdPersonCameraObject.transform.rotation;
            }
        }

        public void SetCameraFieldOfViewTemporary(float value)
        {
            firstPersonCamera.fieldOfView = value;
            thirdPersonCamera.fieldOfView = value;
        }
        public void SetCameraFieldOfView(float value)
        {
            firstPersonCamera.fieldOfView = value;
            thirdPersonCamera.fieldOfView = value;
            defaultFieldOfView = value;
        }
        public float GetCameraFieldOfViewNow()
        {
            return firstPersonCamera.fieldOfView;
        }
        public float GetCameraFieldOfView()
        {
            return defaultFieldOfView;
        }
        public void SetThirdPersonCameraDistanceTemporary(float value)
        {
            SetThirdPersonCameraDistance(value);
            SetThirdPersonCameraRayTargetDistance(value);
            nowCameraDistance = value;
        }
        public float GetThirdPersonCameraDistanceNow()
        {
            return thirdPersonCameraDistance.transform.localScale.x;
        }
        public float GetThirdPersonCameraDistanceDefault()
        {
            return defaultCameraDistance;
        }
        public void SetThirdPersonCameraScreenPosition(Vector2 pos)
        {
            thirdPersonCameraScreenPositionTarget = pos;
        }
        public Vector2 GetThirdPersonCameraScreenPositionNow()
        {
            return thirdPersonCameraScreenPositionTarget.Clone();
        }


        public void UpdateThirdPersonCameraPosition()
        {
            var ray = thirdPersonCameraRayTarget.transform.position - firstPersonCameraObject.transform.position;
            var hitCount = Physics.RaycastNonAlloc(
                firstPersonCameraObject.transform.position,
                ray.normalized,
                raycastHits,
                ray.magnitude,
                ~(1 << 7),  //CameraOnly以外
                QueryTriggerInteraction.Ignore
            );

            if (hitCount == 0)
            {
                SetThirdPersonCameraDistance(nowCameraDistance);
                UpdateThirdPersonCameraScreenPosition(nowCameraDistance);
                return;
            }

            var validHits = raycastHits
                .Take(hitCount)
                .Where(h => !ignoreColliders.Contains(h.collider))
                .OrderBy(h => h.distance)
                .ToArray();

            if (validHits.Length == 0)
            {
                SetThirdPersonCameraDistance(nowCameraDistance);
                UpdateThirdPersonCameraScreenPosition(nowCameraDistance);
                return;
            }

            var distance = validHits[0].distance / ray.magnitude * nowCameraDistance;
            SetThirdPersonCameraDistance(distance);
            UpdateThirdPersonCameraScreenPosition(distance);

        }
        void UpdateThirdPersonCameraScreenPosition(float distance)
        {
            var posCenter = thirdPersonCamera.ScreenToWorldPoint(new Vector3(
                thirdPersonCamera.pixelWidth / 2,
                thirdPersonCamera.pixelHeight / 2,
                distance
            ));
            var posTarget = thirdPersonCamera.ScreenToWorldPoint(new Vector3(
                thirdPersonCamera.pixelWidth * thirdPersonCameraScreenPositionTarget.x,
                thirdPersonCamera.pixelHeight * thirdPersonCameraScreenPositionTarget.y,
                distance
            ));
            var localCenter = thirdPersonCameraDistance.transform.InverseTransformPoint(posCenter);
            var localPos = thirdPersonCameraDistance.transform.InverseTransformPoint(posTarget);
            //視界上の位置じゃなくてカメラの位置なので反転
            thirdPersonCameraScreenPosition.transform.localPosition = -(localPos - localCenter);
            thirdPersonCameraScreenPositionRayTarget.transform.localPosition = -(localPos - localCenter);
        }

        public void ChangeView(bool isFirstPerson)
        {
            if (isFirstPerson)
            {
                this.isFirstPerson = true;
                thirdPersonCamera.enabled = false;
                thirdPersonPpl.enabled = false; //よくわからないタイミングでPPLがエラーになる問題に、これで対応できているか分からない。
                firstPersonCamera.enabled = true;
                firstPersonPpl.enabled = true;
                ToFirstPersonCallback(firstPersonCamera);
            }
            else
            {
                this.isFirstPerson = false;
                thirdPersonCamera.enabled = true;
                thirdPersonPpl.enabled = true;
                firstPersonCamera.enabled = false;
                firstPersonPpl.enabled = false;
                ToThirdPersonCallback(thirdPersonCamera);
            }
        }
    }
}
