using UnityEngine;
using UnityEngine.UI;

namespace NekoLegends
{
    public class NekoDemo : DemoScenes
    {
        [Header("Objects")]
        [SerializeField] private Neko NekoCharacter;
        [SerializeField] private GameObject GoldCoin;

        [Header("Buttons")]
        [SerializeField] private Button PoseBtn;
        [SerializeField] private Button CameraBtn;
        [SerializeField] private Button RotateBtn;
        [SerializeField] private Button NextFeatureBtn;

        [Header("Camera Positions")]
        [SerializeField] private Transform MainCamTransform;
        [SerializeField] private Transform ZoomedCamTransform;

        [Header("Light")]
        [SerializeField] private GameObject PointLight;

        private bool isRotating;
        private bool isZoomedCamera;

        private const string _title = "Cel Shader Demo";

        #region Singleton

        public static NekoDemo Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Object.FindFirstObjectByType<NekoDemo>();

                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        private static NekoDemo _instance;

        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();

            if (PoseBtn != null)
                PoseBtn.onClick.AddListener(PoseBtnClicked);

            if (CameraBtn != null)
                CameraBtn.onClick.AddListener(CameraBtnClicked);

            if (RotateBtn != null)
                RotateBtn.onClick.AddListener(RotateBtnClicked);

            if (NextFeatureBtn != null)
                NextFeatureBtn.onClick.AddListener(NextBtnClicked);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (PoseBtn != null)
                PoseBtn.onClick.RemoveListener(PoseBtnClicked);

            if (CameraBtn != null)
                CameraBtn.onClick.RemoveListener(CameraBtnClicked);

            if (RotateBtn != null)
                RotateBtn.onClick.RemoveListener(RotateBtnClicked);

            if (NextFeatureBtn != null)
                NextFeatureBtn.onClick.RemoveListener(NextBtnClicked);
        }

        protected override void Start()
        {
            base.Start();

            CameraBtnClicked();

            if (GoldCoin != null)
                GoldCoin.SetActive(false);

            if (DescriptionText != null)
                DescriptionText.SetText(_title);
        }

        private void NextBtnClicked()
        {
            if (NekoCharacter == null)
                return;

            int featureIndex = NekoCharacter.ShowNextFeature();

            if (featureIndex == NekoCharacter.GetFeatureCount() - 1)
            {
                NekoCharacter.gameObject.SetActive(false);

                if (GoldCoin != null)
                    GoldCoin.SetActive(true);

                if (DescriptionText != null)
                    DescriptionText.SetText("Texture Normals");

                isRotating = true;
            }
            else if (featureIndex == 0)
            {
                NekoCharacter.gameObject.SetActive(true);

                if (GoldCoin != null)
                    GoldCoin.SetActive(false);

                NekoCharacter.transform.localPosition = Vector3.zero;
                NekoCharacter.transform.localRotation = Quaternion.identity;

                isRotating = false;
            }
        }

        private void RotateBtnClicked()
        {
            isRotating = !isRotating;
        }

        private void PoseBtnClicked()
        {
            if (NekoCharacter != null)
                NekoCharacter.NextPose();
        }

        private void CameraBtnClicked()
        {
            FlyToNextCameraHandler();
        }

        private void FlyToNextCameraHandler()
        {
            if (Camera.main == null)
                return;

            isZoomedCamera = !isZoomedCamera;

            Transform targetCameraTransform = isZoomedCamera ? ZoomedCamTransform : MainCamTransform;

            if (targetCameraTransform == null)
                return;

            Camera.main.transform.position = targetCameraTransform.position;
            Camera.main.transform.rotation = targetCameraTransform.rotation;
        }

        public void SetBackgroundActive(bool isOn)
        {
            if (BGTransform != null)
                BGTransform.gameObject.SetActive(isOn);
        }

        public void SetPointLightActive(bool isOn)
        {
            if (PointLight != null)
                PointLight.SetActive(isOn);
        }

        private void Update()
        {
            if (!isRotating)
                return;

            float rotationSpeed = 50f;

            if (NekoCharacter != null && NekoCharacter.gameObject.activeSelf)
                NekoCharacter.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            if (GoldCoin != null && GoldCoin.activeSelf)
                GoldCoin.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}